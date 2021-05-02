using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Migrations;
using System;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Pass first argument with connection string to database, second with file storage type (fs/azure), third a connection to azure storage or local path template (relative path not supported).");
                return;
            }

            string connectionString = args[0];

            Console.WriteLine("Creating context.");
            using var entries = new EntriesDataContext(DbContextOptions<EntriesDataContext>(connectionString, "Entries"), Schema<EntriesDataContext>("Entries"));

            var imageFormat = ImageFormatDefinition.Jpeg;

            IFileStorage fileStorage = null;
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
                .Where(i => i.OriginalWidth == 0 || i.OriginalHeight == 0)
                .ToListAsync();

            Console.WriteLine($"Found '{images.Count}' images.");
            foreach (var image in images)
            {
                var fileContent = await fileStorage.FindAsync(image.Entry, image, ImageType.Original);
                if (fileContent != null)
                {
                    using (fileContent)
                    {
                        var size = resizeService.GetSize(fileContent);
                        image.OriginalWidth = size.width;
                        image.OriginalHeight = size.height;

                        entries.Images.Update(image);
                    }
                }
            }

            Console.WriteLine("Saving changes.");
            await entries.SaveChangesAsync();

            Console.WriteLine("Done.");
        }

        private static DbContextOptions<T> DbContextOptions<T>(string connectionString, string schema)
            where T : DbContext
        {
            if (connectionString.StartsWith("Filename"))
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
                        if (!String.IsNullOrEmpty(schema))
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
}
