using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Neptuo.Recollections.Entries;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Tests.Infrastructure;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection accountsConnection;
    private readonly SqliteConnection entriesConnection;

    public ApiFactory()
    {
        accountsConnection = new SqliteConnection("DataSource=:memory:");
        accountsConnection.Open();

        entriesConnection = new SqliteConnection("DataSource=:memory:");
        entriesConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide configuration so the original startup doesn't fail on missing values
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Accounts:Database:Server"] = "Sqlite",
                ["Accounts:Database:ConnectionString"] = "DataSource=:memory:",
                ["Accounts:Jwt:SecurityKey"] = "test-key-that-is-at-least-32-chars-long",
                ["Accounts:Jwt:Issuer"] = "https://test",
                ["Accounts:Jwt:ExpiryInDays"] = "1",
                ["Accounts:Identity:Password:RequireDigit"] = "false",
                ["Accounts:Identity:Password:RequiredLength"] = "4",
                ["Accounts:Identity:Password:RequireLowercase"] = "false",
                ["Accounts:Identity:Password:RequireNonAlphanumeric"] = "false",
                ["Accounts:Identity:Password:RequireUppercase"] = "false",
                ["Entries:Database:Server"] = "Sqlite",
                ["Entries:Database:ConnectionString"] = "DataSource=:memory:",
                ["Entries:Storage:FileSystem:PathTemplate"] = Path.Combine(Path.GetTempPath(), "recollections-tests", "{UserId}", "{EntryId}"),
                ["Cors:Origins:0"] = "http://localhost",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            services.RemoveAll<DbContextOptions<AccountsDataContext>>();
            services.RemoveAll<DbContextOptions<EntriesDataContext>>();

            // Re-register with in-memory SQLite
            services.AddDbContext<AccountsDataContext>(options =>
                options.UseSqlite(accountsConnection));

            services.AddDbContext<EntriesDataContext>(options =>
                options.UseSqlite(entriesConnection));

            // Ensure SchemaOptions are registered (needed by DbContext constructors)
            services.TryAddSingleton(SchemaOptions<AccountsDataContext>.Default);
            services.TryAddSingleton(SchemaOptions<EntriesDataContext>.Default);

            // Replace auth with test handler
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options => { });

            services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(TestAuthHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build());

            // Replace file storage with a temp directory
            services.Configure<SystemIoStorageOptions>(options =>
            {
                options.PathTemplate = Path.Combine(Path.GetTempPath(), "recollections-tests", Guid.NewGuid().ToString("N"), "{UserId}", "{EntryId}");
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        ApplyMigrations<AccountsDataContext>(scope.ServiceProvider);
        ApplyMigrations<EntriesDataContext>(scope.ServiceProvider);

        return host;
    }

    private static void ApplyMigrations<TContext>(IServiceProvider serviceProvider)
        where TContext : DbContext
    {
        var dbContext = serviceProvider.GetRequiredService<TContext>();
        dbContext.Database.Migrate();
    }

    /// <summary>
    /// Creates an HttpClient authenticated as the given user.
    /// </summary>
    public HttpClient CreateClientForUser(string userId, string userName)
    {
        var client = CreateClient();
        client.SetUser(userId, userName);
        return client;
    }

    /// <summary>
    /// Creates an unauthenticated HttpClient.
    /// </summary>
    public HttpClient CreateAnonymousClient()
    {
        var client = CreateClient();
        client.SetAnonymous();
        return client;
    }

    /// <summary>
    /// Runs an action against both database contexts within a DI scope.
    /// Uses the key to ensure each seeder runs only once per factory instance.
    /// </summary>
    private readonly HashSet<string> seededKeys = new();
    private readonly SemaphoreSlim seedLock = new(1, 1);

    public async Task SeedAsync(string key, Func<AccountsDataContext, EntriesDataContext, Task> seeder)
    {
        await seedLock.WaitAsync();
        try
        {
            if (seededKeys.Contains(key))
                return;

            using var scope = Services.CreateScope();
            var accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
            var entriesDb = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();
            await seeder(accountsDb, entriesDb);
            seededKeys.Add(key);
        }
        finally
        {
            seedLock.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            accountsConnection.Dispose();
            entriesConnection.Dispose();
        }
    }
}
