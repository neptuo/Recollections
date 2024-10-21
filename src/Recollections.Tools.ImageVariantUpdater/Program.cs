using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo.Recollections;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Migrations;
using System;
using System.Linq;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

if (!TryGetConfiguration(args, out var dbConnectionString, out var storageType, out var storageConnectionString, out var isDryRun, out var thresholdDate))
    return;

var imageFormat = ImageFormatDefinition.Jpeg;
IFileStorage fileStorage = null;
if (storageType == "fs")
{
    fileStorage = new SystemIoFileStorage(path => path, Options.Create(new SystemIoStorageOptions() { PathTemplate = storageConnectionString }), imageFormat);
}
else if (storageType == "azure")
{
    fileStorage = new AzureFileStorage(Options.Create(new AzureStorageOptions() { ConnectionString = storageConnectionString }));
}
else
{
    Console.WriteLine($"Not supported type of file storage '{storageType}'.");
    return;
}

Console.WriteLine("Creating context.");
using var db = new EntriesDataContext(DbContextOptions<EntriesDataContext>(dbConnectionString, "Entries"), Schema<EntriesDataContext>("Entries"));
var imageService = new ImageService(db, fileStorage, null, new ImageResizeService(imageFormat), null);

Console.WriteLine("Preparing the query.");

// TODO: Implement
var images = await db.Images
    .Where(i => i.Created >= thresholdDate)
    .Include(i => i.Entry)
    .AsNoTracking()
    .ToListAsync();

Console.WriteLine($"Found '{images.Count}' matching images");
foreach (var image in images)
{
    Console.WriteLine($"Recalculating for image '{image.Id}'");
    await imageService.ComputeOtherSizesAsync(image.Entry, image);
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

static bool TryGetConfiguration(string[] args, out string connectionString, out string storageType, out string storageConnectionString, out bool isDryRun, out DateTime thresholdDate)
{
    connectionString = null;
    storageType = null;
    storageConnectionString = null;
    isDryRun = false;
    thresholdDate = DateTime.MinValue;

    if (args.Length != 3)
    {
        Console.WriteLine("Pass three arguments.");
        return false;
    }

    connectionString = args[0];
    storageType = args[1];
    storageConnectionString = args[2];
    return true;
}
