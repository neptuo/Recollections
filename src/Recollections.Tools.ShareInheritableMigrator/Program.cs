using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections;
using Neptuo.Recollections.Migrations;
using System;
using System.Linq;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

if (!TryGetConfiguration(args, out var connectionString, out var isDryRun, out var skippedEntryIds))
    return;

Console.WriteLine("Creating context.");
using var db = new EntriesDataContext(DbContextOptions<EntriesDataContext>(connectionString, "Entries"), Schema<EntriesDataContext>("Entries"));

Console.WriteLine("Preparing the query.");
var entries = await db.Entries
    .Where(e => !e.IsSharingInherited
        && !skippedEntryIds.Contains(e.Id)
        && !db.EntryShares.Any(s => s.EntryId == e.Id)
        && (
            (e.Story != null && !e.Story.IsSharingInherited)
            || (e.Chapter != null && e.Chapter.Story.IsSharingInherited)
            || e.Beings.Any(b => (b.Id != b.UserId && !b.IsSharingInherited))
        )
    )
    .Include(e => e.Story)
    .Include(e => e.Chapter)
    .AsNoTracking()
    .ToListAsync();

Console.WriteLine($"Found '{entries.Count}' entries matching the criteria");
foreach (var e in entries)
{
    Console.WriteLine($"Entry '{e.Id}' marked as 'sharing inherited'");
    Console.WriteLine($"    {e.Title}");
    Console.WriteLine($"    {e.UserId}");

    var entry = db.Entries.Find(e.Id);
    entry.IsSharingInherited = true;
    db.Entries.Update(entry);
}

if (!isDryRun)
{
    Console.WriteLine("Saving changes.");
    await db.SaveChangesAsync();
}

Console.WriteLine("Done.");

static DbContextOptions<T> DbContextOptions<T>(string connectionString, string schema)
    where T : DbContext
{
    var builder = new DbContextOptionsBuilder<T>()
        .UseSqlServer(connectionString, sql =>
        {
            if (!String.IsNullOrEmpty(schema))
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

static bool TryGetConfiguration(string[] args, out string connectionString, out bool isDryRun, out string[] skippedEntryIds)
{
    connectionString = null;
    isDryRun = false;
    skippedEntryIds = [];

    if (args.Length != 1)
    {
        Console.WriteLine("Pass one argument with connection string to database.");
        return false;
    }

    connectionString = args[0];
    return true;
}