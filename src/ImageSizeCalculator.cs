#:sdk Microsoft.NET.Sdk
#:property PublishAot=false
#:project ./Recollections.Accounts.Data/Recollections.Accounts.Data.csproj
#:project ./Recollections.Data.Ef/Recollections.Data.Ef.csproj
#:project ./Recollections.Entries/Recollections.Entries.csproj
#:project ./Recollections.Entries.Azure/Recollections.Entries.Azure.csproj
#:project ./Recollections.Entries.Data/Recollections.Entries.Data.csproj
#:project ./Recollections.Entries.SystemIo/Recollections.Entries.SystemIo.csproj

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo.Recollections;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Migrations;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

await new ImageSizeCalculator().RunAsync(args);

internal sealed class ImageSizeCalculator
{
    public async Task RunAsync(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Pass first argument with connection string to database, second with file storage type (fs/azure), third a connection to azure storage or local path template (relative path not supported).");
            return;
        }

        string connectionString = args[0];

        Console.WriteLine("Creating context.");
        using var entries = new EntriesDataContext(
            DbContextOptions<EntriesDataContext>(connectionString, "Entries"),
            Schema<EntriesDataContext>("Entries"));

        var imageFormat = ImageFormatDefinition.Jpeg;

        IFileStorage fileStorage;
        string storageType = args[1];
        if (storageType == "fs")
        {
            fileStorage = new SystemIoFileStorage(path => path, Options.Create(new SystemIoStorageOptions() { PathTemplate = args[2] }), imageFormat);
        }
        else if (storageType == "azure")
        {
            fileStorage = new AzureFileStorage(Options.Create(new AzureStorageOptions() { ConnectionString = args[2] }));
        }
        else
        {
            Console.WriteLine($"Not supported type of file storage '{storageType}'.");
            return;
        }

        var resizeService = new ImageResizeService(imageFormat);

        Console.WriteLine("Getting images.");
        var images = await entries.Images
            .Include(i => i.Entry)
            .ToListAsync();

        Console.WriteLine($"Found '{images.Count}' images.");
        foreach (var image in images)
        {
            if (!await TryUpdateOriginalSizeAsync(entries, fileStorage, resizeService, image, ImageType.Original))
                await TryUpdateOriginalSizeAsync(entries, fileStorage, resizeService, image, ImageType.Preview);
        }

        Console.WriteLine("Saving changes.");
        await entries.SaveChangesAsync();

        Console.WriteLine("Done.");
    }

    private static async Task<bool> TryUpdateOriginalSizeAsync(EntriesDataContext entries, IFileStorage fileStorage, ImageResizeService resizeService, Image image, ImageType imageType)
    {
        var fileContent = await fileStorage.FindAsync(image.Entry, image, imageType);
        if (fileContent != null)
        {
            using (fileContent)
            {
                var size = resizeService.GetSize(fileContent);
                if (size.width != image.OriginalWidth || size.height != image.OriginalHeight)
                {
                    image.OriginalWidth = size.width;
                    image.OriginalHeight = size.height;

                    entries.Images.Update(image);
                }
            }

            return true;
        }
        else
        {
            Console.WriteLine($"Missing '{imageType}' file for '{image.Id}'.");
            return false;
        }
    }

    private static DbContextOptions<T> DbContextOptions<T>(string connectionString, string schema)
        where T : DbContext
    {
        if (connectionString.StartsWith("Filename", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new DbContextOptionsBuilder<T>()
                .UseSqlite(connectionString);

            return builder.Options;
        }
        else
        {
            var builder = new DbContextOptionsBuilder<T>()
                .UseSqlServer(connectionString, sql =>
                {
                    if (!string.IsNullOrEmpty(schema))
                        sql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
                });

            return builder.Options;
        }
    }

    private static SchemaOptions<T> Schema<T>(string name)
        where T : DbContext
    {
        var schema = new SchemaOptions<T>() { Name = name };
        MigrationWithSchema.SetSchema(schema);
        return schema;
    }
}
