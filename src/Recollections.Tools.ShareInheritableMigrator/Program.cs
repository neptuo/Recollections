using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections;
using Neptuo.Recollections.Migrations;
using System;
using System.Linq;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

if (args.Length != 1)
{
    Console.WriteLine("Pass one argument with connection string to database.");
    return;
}

string connectionString = args[0];

Console.WriteLine("Creating context.");
using var db = new EntriesDataContext(DbContextOptions<EntriesDataContext>(connectionString, "Entries"), Schema<EntriesDataContext>("Entries"));

// TODO: Implement
var entries = await db.Entries
    .Where(e => !e.IsSharingInherited 
        && (
            (e.Story != null && !e.Story.IsSharingInherited)
            || (e.Chapter != null && e.Chapter.Story.IsSharingInherited)
            || e.Beings.Any(b => (b.Id != b.UserId && !b.IsSharingInherited))
        )
    )
    .Include(e => e.Story)
    .Include(e => e.Chapter)
    .ToListAsync();

Console.WriteLine($"Found '{entries.Count}' entries matching the criteria");
foreach (var entry in entries)
{
    entry.IsSharingInherited = true;
    System.Console.WriteLine($"Entry '{entry.Id}' marked as 'sharing inherited'");
    db.Entries.Update(entry);
}

await db.SaveChangesAsync();
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
