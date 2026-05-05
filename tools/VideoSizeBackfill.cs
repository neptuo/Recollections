#:sdk Microsoft.NET.Sdk
#:property PublishAot=false
#:project ../src/Recollections.Data.Ef/Recollections.Data.Ef.csproj
#:project ../src/Recollections.Entries/Recollections.Entries.csproj
#:project ../src/Recollections.Entries.Azure/Recollections.Entries.Azure.csproj
#:project ../src/Recollections.Entries.Data/Recollections.Entries.Data.csproj
#:project ../src/Recollections.Entries.SystemIo/Recollections.Entries.SystemIo.csproj

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo.Recollections;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Migrations;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

if (args.Length != 3)
{
    Console.WriteLine("Pass first argument with connection string to database, second with file storage type (fs/azure), third a connection to azure storage or local path template (relative path not supported).");
    return;
}

// To bypass command-line arguments, replace with hardcoded values:
string connectionString = args[0];
string storageType = args[1];
string storagePath = args[2];

Console.WriteLine("Creating context.");
using var entries = new EntriesDataContext(
    DbContextOptions<EntriesDataContext>(connectionString, "Entries"),
    Schema<EntriesDataContext>("Entries"));

IFileStorage? fileStorage = CreateFileStorage(storageType, storagePath);
if (fileStorage == null)
    return;

Console.WriteLine("Getting videos missing size.");
var videos = await entries.Videos
    .Include(v => v.Entry)
    .Where(v => v.OriginalSize == null)
    .ToListAsync();

Console.WriteLine($"Found '{videos.Count}' videos.");
foreach (var video in videos)
{
    using Stream? content = await fileStorage.FindAsync(video.Entry, video, VideoType.Original);
    if (content == null)
    {
        Console.WriteLine($"Missing original file for '{video.Id}'.");
        continue;
    }

    long size = await GetLengthAsync(content);
    video.OriginalSize = size;
    Console.WriteLine($"Video '{video.Id}' updated to {size} bytes.");
}

Console.WriteLine("Saving changes.");
await entries.SaveChangesAsync();
Console.WriteLine("Done.");


static IFileStorage? CreateFileStorage(string storageType, string storagePathOrConnectionString)
{
    if (storageType == "fs")
    {
        return new SystemIoFileStorage(
            path => path,
            Options.Create(new SystemIoStorageOptions() { PathTemplate = storagePathOrConnectionString }),
            ImageFormatDefinition.Jpeg);
    }

    if (storageType == "azure")
        return new AzureFileStorage(Options.Create(new AzureStorageOptions() { ConnectionString = storagePathOrConnectionString }));

    Console.WriteLine($"Not supported type of file storage '{storageType}'.");
    return null;
}

static async Task<long> GetLengthAsync(Stream content)
{
    if (content.CanSeek)
        return content.Length;

    long result = 0;
    byte[] buffer = new byte[81920];
    int read;
    while ((read = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
        result += read;

    return result;
}

static DbContextOptions<T> DbContextOptions<T>(string connectionString, string schema)
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

static SchemaOptions<T> Schema<T>(string name)
    where T : DbContext
{
    var schema = new SchemaOptions<T>() { Name = name };
    MigrationWithSchema.SetSchema(schema);
    return schema;
}
