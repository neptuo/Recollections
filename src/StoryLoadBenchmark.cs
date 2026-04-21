#:sdk Microsoft.NET.Sdk
#:property PublishAot=false
#:project ./Recollections.Api/Recollections.Api.csproj
#:project ./Recollections.Accounts/Recollections.Accounts.csproj
#:project ./Recollections.Accounts.Data/Recollections.Accounts.Data.csproj
#:project ./Recollections.Data.Ef/Recollections.Data.Ef.csproj
#:project ./Recollections.Entries/Recollections.Entries.csproj
#:project ./Recollections.Entries.Data/Recollections.Entries.Data.csproj
#:project ./Recollections.Entries.SystemIo/Recollections.Entries.SystemIo.csproj
#:project ./Recollections.SystemIo/Recollections.SystemIo.csproj

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neptuo.Recollections;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

await new StoryLoadBenchmark().RunAsync(args);

internal sealed class StoryLoadBenchmark
{
    private const int WarmupIterations = 1;
    private const int MeasuredIterations = 5;

    public async Task RunAsync(string[] args)
    {
        int iterations = MeasuredIterations;
        if (args.Length > 0 && int.TryParse(args[0], out int parsed) && parsed > 0)
            iterations = parsed;

        string repositoryRoot = FindRepositoryRoot();
        string apiRoot = Path.Combine(repositoryRoot, "src", "Recollections.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiRoot)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, configuration, apiRoot);

        using var provider = services.BuildServiceProvider();

        // Find user 'jondoe'
        using (var scope = provider.CreateScope())
        {
            var accountsDb = scope.ServiceProvider.GetRequiredService<AccountsDataContext>();
            var entriesDb = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();

            var user = await accountsDb.Users.FirstOrDefaultAsync(u => u.UserName == "jondoe");
            if (user == null)
            {
                Console.WriteLine("Error: user 'jondoe' not found. Run SampleDataSeeder with --large (or --story-entries <count>) first.");
                return;
            }

            // Find the largest story for jondoe
            var storyInfo = await entriesDb.Stories
                .Where(s => s.UserId == user.Id)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    EntryCount = entriesDb.Entries.Count(e => e.Story.Id == s.Id)
                })
                .OrderByDescending(s => s.EntryCount)
                .FirstOrDefaultAsync();

            if (storyInfo == null)
            {
                Console.WriteLine("Error: no stories found for 'jondoe'.");
                return;
            }

            Console.WriteLine($"Story: '{storyInfo.Title}' ({storyInfo.EntryCount} entries)");
            Console.WriteLine($"Iterations: {WarmupIterations} warmup + {iterations} measured");
            Console.WriteLine();

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                using var warmupScope = provider.CreateScope();
                await RunTimelineQuery(warmupScope.ServiceProvider, user.Id, storyInfo.Id);
            }

            // Measured
            var timings = new List<BenchmarkTimings>(iterations);
            for (int i = 0; i < iterations; i++)
            {
                using var measuredScope = provider.CreateScope();
                var timing = await RunTimelineQuery(measuredScope.ServiceProvider, user.Id, storyInfo.Id);
                timings.Add(timing);
                Console.WriteLine($"  Run {i + 1}: {timing}");
            }

            Console.WriteLine();
            PrintSummary(timings);
        }
    }

    private static async Task<BenchmarkTimings> RunTimelineQuery(IServiceProvider services, string userId, string storyId)
    {
        var entriesDb = services.GetRequiredService<EntriesDataContext>();
        var shareStatus = services.GetRequiredService<ShareStatusService>();
        var connections = services.GetRequiredService<IConnectionProvider>();
        var entryMapper = services.GetRequiredService<EntryListMapper>();

        var timings = new BenchmarkTimings();
        var total = Stopwatch.StartNew();

        // Step 1: Get connected users (same as controller)
        var stepWatch = Stopwatch.StartNew();
        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        stepWatch.Stop();
        timings.ConnectionLookup = stepWatch.Elapsed;

        // Step 2: Build the query with permission filter (same as StoryEntriesController.GetStoryTimeline)
        stepWatch.Restart();
        var query = shareStatus.OwnedByOrExplicitlySharedWithUser(
            entriesDb,
            entriesDb.Entries
                .Where(e => e.Story.Id == storyId)
                .OrderBy(e => e.When),
            [userId, ShareStatusService.PublicUserId],
            connectedUsers
        );
        stepWatch.Stop();
        timings.QueryBuild = stepWatch.Elapsed;

        // Step 3: Execute the full mapping pipeline (entry projection, beings, preview media, user names)
        stepWatch.Restart();
        var (models, hasMore) = await entryMapper.MapAsync(query, [userId], connectedUsers, includePreviewMedia: true);
        stepWatch.Stop();
        timings.MapAsync = stepWatch.Elapsed;

        total.Stop();
        timings.Total = total.Elapsed;
        timings.EntryCount = models.Count;

        return timings;
    }

    private static void PrintSummary(List<BenchmarkTimings> timings)
    {
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"  Entries returned: {timings[0].EntryCount}");
        Console.WriteLine($"  Total           avg={Avg(timings, t => t.Total):F1}ms  min={Min(timings, t => t.Total):F1}ms  max={Max(timings, t => t.Total):F1}ms");
        Console.WriteLine($"  ConnectionLookup avg={Avg(timings, t => t.ConnectionLookup):F1}ms  min={Min(timings, t => t.ConnectionLookup):F1}ms  max={Max(timings, t => t.ConnectionLookup):F1}ms");
        Console.WriteLine($"  QueryBuild       avg={Avg(timings, t => t.QueryBuild):F1}ms  min={Min(timings, t => t.QueryBuild):F1}ms  max={Max(timings, t => t.QueryBuild):F1}ms");
        Console.WriteLine($"  MapAsync         avg={Avg(timings, t => t.MapAsync):F1}ms  min={Min(timings, t => t.MapAsync):F1}ms  max={Max(timings, t => t.MapAsync):F1}ms");
    }

    private static double Avg(List<BenchmarkTimings> timings, Func<BenchmarkTimings, TimeSpan> selector)
        => timings.Average(t => selector(t).TotalMilliseconds);
    private static double Min(List<BenchmarkTimings> timings, Func<BenchmarkTimings, TimeSpan> selector)
        => timings.Min(t => selector(t).TotalMilliseconds);
    private static double Max(List<BenchmarkTimings> timings, Func<BenchmarkTimings, TimeSpan> selector)
        => timings.Max(t => selector(t).TotalMilliseconds);

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, string apiRoot)
    {
        PathResolver pathResolver = relativePath => relativePath.Replace("{BasePath}", apiRoot, StringComparison.Ordinal);

        services
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning))
            .AddSingleton<PathResolver>(pathResolver)
            .AddDbContextWithSchema<AccountsDataContext>(configuration.GetSection("Accounts").GetSection("Database"), pathResolver)
            .AddDbContextWithSchema<EntriesDataContext>(configuration.GetSection("Entries").GetSection("Database"), pathResolver)
            .AddIdentityCore<User>(options => configuration.GetSection("Accounts").GetSection("Identity").GetSection("Password").Bind(options.Password))
            .AddEntityFrameworkStores<AccountsDataContext>();

        services
            .AddTransient<IUserNameProvider, DbUserNameProvider>()
            .AddTransient<IConnectionProvider, DbConnectionProvider>()
            .AddTransient<IUserPremiumProvider, DbUserPremiumProvider>()
            .AddTransient<IImageValidator, PremiumImageSizeValidator>()
            .AddTransient<IVideoValidator, VideoValidator>()
            .AddTransient<FreeLimitsChecker>()
            .AddTransient<ImageResizeService>()
            .AddTransient<ImageService>()
            .AddTransient<VideoService>()
            .AddTransient<EntryMediaMapper>()
            .AddTransient<EntryListMapper>()
            .AddTransient<ShareStatusService>()
            .AddTransient<IFileStorage, SystemIoFileStorage>()
            .Configure<StorageOptions>(configuration.GetSection("Entries").GetSection("Storage"))
            .Configure<SystemIoStorageOptions>(configuration.GetSection("Entries").GetSection("Storage").GetSection("FileSystem"))
            .AddSingleton(ImageFormatDefinition.Jpeg);
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(Directory.GetCurrentDirectory()); current != null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "src", "Recollections.Api", "appsettings.Development.json")))
                return current.FullName;
        }

        throw new InvalidOperationException("Unable to locate the repository root.");
    }

    private sealed class BenchmarkTimings
    {
        public TimeSpan Total { get; set; }
        public TimeSpan ConnectionLookup { get; set; }
        public TimeSpan QueryBuild { get; set; }
        public TimeSpan MapAsync { get; set; }
        public int EntryCount { get; set; }

        public override string ToString()
            => $"total={Total.TotalMilliseconds:F1}ms (conn={ConnectionLookup.TotalMilliseconds:F1}ms, build={QueryBuild.TotalMilliseconds:F1}ms, map={MapAsync.TotalMilliseconds:F1}ms) entries={EntryCount}";
    }
}
