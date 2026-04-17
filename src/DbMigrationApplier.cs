#:sdk Microsoft.NET.Sdk
#:property PublishAot=false
#:project ./Recollections.Accounts.Data/Recollections.Accounts.Data.csproj
#:project ./Recollections.Data.Ef/Recollections.Data.Ef.csproj
#:project ./Recollections.Entries.Data/Recollections.Entries.Data.csproj

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Neptuo.Recollections;
using Neptuo.Recollections.Migrations;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

string connectionString;
if (args.Length == 0)
{
    Console.WriteLine("Enter connection string to database to migrate:");
    connectionString = Console.ReadLine();
}
else if (args.Length != 1)
{
    Console.WriteLine("Pass one argument with connection string to database to migrate.");
    return;
}
else
{
    connectionString = args[0];
}

Console.WriteLine("Creating contexts.");
using var accounts = new AccountsDataContext(DbContextOptions<AccountsDataContext>(connectionString, "Accounts"), Schema<AccountsDataContext>("Accounts"));
using var entries = new EntriesDataContext(DbContextOptions<EntriesDataContext>(connectionString, "Entries"), Schema<EntriesDataContext>("Entries"));

Console.WriteLine("Migrating accounts db.");
accounts.Database.Migrate();

Console.WriteLine("Migrating entries db.");
entries.Database.Migrate();

Console.WriteLine("Done.");

static DbContextOptions<T> DbContextOptions<T>(string connectionString, string schema)
    where T : DbContext
{
    var builder = new DbContextOptionsBuilder<T>()
        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
        .UseSqlServer(connectionString, sql =>
        {
            if (!string.IsNullOrEmpty(schema))
                sql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
        });

    return builder.Options;
}

static SchemaOptions<T> Schema<T>(string name)
    where T : DbContext
{
    var schema = new SchemaOptions<T>() { Name = name };
    MigrationWithSchema.SetSchema(schema);
    return schema;
}
