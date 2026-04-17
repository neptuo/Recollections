#:sdk Microsoft.NET.Sdk
#:property PublishAot=false
#:project ./Recollections.Data.Ef/Recollections.Data.Ef.csproj
#:project ./Recollections.Entries/Recollections.Entries.csproj
#:project ./Recollections.Entries.Azure/Recollections.Entries.Azure.csproj
#:project ./Recollections.Entries.Data/Recollections.Entries.Data.csproj
#:project ./Recollections.Entries.SystemIo/Recollections.Entries.SystemIo.csproj

using ExifLib;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo.Recollections;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Migrations;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

await new AltitudeBackfill().RunAsync(args);

internal sealed class AltitudeBackfill
{
    private int metadataConfirmed;
    private int metadataDiscrepancy;
    private int metadataUnavailable;

    public async Task RunAsync(string[] args)
    {
        if (!TryGetConfiguration(args, out string dbConnectionString, out string storageType, out string storageConnectionString, out bool isDryRun))
            return;

        IFileStorage fileStorage = null;
        if (storageType != null)
        {
            fileStorage = CreateFileStorage(storageType, storageConnectionString);
            if (fileStorage == null)
                return;
        }

        Console.WriteLine("Creating context.");
        using var db = new EntriesDataContext(
            DbContextOptions<EntriesDataContext>(dbConnectionString, "Entries"),
            Schema<EntriesDataContext>("Entries"));

        int invalidCount = 0;
        int fixedCount = 0;

        Console.WriteLine("Scanning images.");
        var invalidImages = await db.Images
            .Where(i => i.Location.Altitude != null
                && (i.Location.Altitude < AltitudeBounds.MinMeters || i.Location.Altitude > AltitudeBounds.MaxMeters))
            .Include(i => i.Entry)
            .ToListAsync();

        Console.WriteLine($"Found '{invalidImages.Count}' images with out-of-bounds altitude.");
        foreach (var image in invalidImages)
        {
            invalidCount++;
            Console.WriteLine($"  Image '{image.Id}' (entry '{image.Entry.Id}'): altitude = {image.Location.Altitude}");

            if (fileStorage != null)
                await VerifyImageMetadataAsync(fileStorage, image);

            image.Location.Altitude = null;
            fixedCount++;
        }

        Console.WriteLine("Scanning videos.");
        var invalidVideos = await db.Videos
            .Where(v => v.Location.Altitude != null
                && (v.Location.Altitude < AltitudeBounds.MinMeters || v.Location.Altitude > AltitudeBounds.MaxMeters))
            .Include(v => v.Entry)
            .ToListAsync();

        Console.WriteLine($"Found '{invalidVideos.Count}' videos with out-of-bounds altitude.");
        foreach (var video in invalidVideos)
        {
            invalidCount++;
            Console.WriteLine($"  Video '{video.Id}' (entry '{video.Entry.Id}'): altitude = {video.Location.Altitude}");
            video.Location.Altitude = null;
            fixedCount++;
        }

        Console.WriteLine("Scanning entry locations.");
        var entriesWithInvalidOrderedLocations = await db.Entries
            .Where(e => e.Locations.Any(l => l.Altitude != null
                && (l.Altitude < AltitudeBounds.MinMeters || l.Altitude > AltitudeBounds.MaxMeters)))
            .ToListAsync();

        Console.WriteLine($"Found '{entriesWithInvalidOrderedLocations.Count}' entries with out-of-bounds ordered-location altitude.");
        foreach (var entry in entriesWithInvalidOrderedLocations)
        {
            foreach (var location in entry.Locations.Where(l => !AltitudeBounds.IsValid(l.Altitude)))
            {
                invalidCount++;
                Console.WriteLine($"  Entry '{entry.Id}' location #{location.Order}: altitude = {location.Altitude}");
                location.Altitude = null;
                fixedCount++;
            }
        }

        Console.WriteLine("Scanning entry track altitudes.");
        var invalidTracks = await db.Entries
            .Where(e => e.TrackAltitude != null
                && (e.TrackAltitude < AltitudeBounds.MinMeters || e.TrackAltitude > AltitudeBounds.MaxMeters))
            .ToListAsync();

        Console.WriteLine($"Found '{invalidTracks.Count}' entries with out-of-bounds track altitude.");
        foreach (var entry in invalidTracks)
        {
            invalidCount++;
            Console.WriteLine($"  Entry '{entry.Id}': TrackAltitude = {entry.TrackAltitude}");
            entry.TrackAltitude = null;
            fixedCount++;
        }

        Console.WriteLine();
        Console.WriteLine($"Summary: {invalidCount} invalid altitude(s) found, {fixedCount} {(isDryRun ? "would be unset" : "unset")}.");
        if (fileStorage != null)
            Console.WriteLine($"Photo metadata: {metadataConfirmed} confirmed invalid, {metadataDiscrepancy} appeared valid in EXIF, {metadataUnavailable} unavailable.");

        if (isDryRun)
        {
            Console.WriteLine("Dry-run: no changes saved.");
        }
        else if (fixedCount > 0)
        {
            Console.WriteLine("Saving changes.");
            await db.SaveChangesAsync();
        }

        Console.WriteLine("Done.");
    }

    private async Task VerifyImageMetadataAsync(IFileStorage fileStorage, Image image)
    {
        Stream stream = null;
        try
        {
            stream = await fileStorage.FindAsync(image.Entry, image, ImageType.Original);
            if (stream == null)
            {
                metadataUnavailable++;
                Console.WriteLine("    metadata: original not found in storage");
                return;
            }

            var result = ReadExifAltitude(stream);
            if (!result.Success)
            {
                metadataUnavailable++;
                Console.WriteLine("    metadata: failed to read EXIF");
                return;
            }

            var exifAltitude = result.Altitude;
            if (exifAltitude == null)
            {
                metadataConfirmed++;
                Console.WriteLine("    metadata: no altitude in EXIF (confirmed invalid import)");
            }
            else if (!AltitudeBounds.IsValid(exifAltitude))
            {
                metadataConfirmed++;
                Console.WriteLine($"    metadata: EXIF altitude {exifAltitude} is also out of bounds (confirmed)");
            }
            else
            {
                metadataDiscrepancy++;
                Console.WriteLine($"    metadata: EXIF altitude {exifAltitude} looks valid — import pipeline may have corrupted the value");
            }
        }
        catch (Exception ex)
        {
            metadataUnavailable++;
            Console.WriteLine($"    metadata: failed to read original ({ex.GetType().Name}: {ex.Message})");
        }
        finally
        {
            if (stream != null)
                await stream.DisposeAsync();
        }
    }

    private readonly struct ExifAltitudeResult
    {
        public ExifAltitudeResult(bool success, double? altitude)
        {
            Success = success;
            Altitude = altitude;
        }

        public bool Success { get; }
        public double? Altitude { get; }
    }

    private static ExifAltitudeResult ReadExifAltitude(Stream content)
    {
        Stream seekable = content;
        MemoryStream buffered = null;
        if (!content.CanSeek)
        {
            buffered = new MemoryStream();
            content.CopyTo(buffered);
            buffered.Position = 0;
            seekable = buffered;
        }
        else
        {
            content.Position = 0;
        }

        try
        {
            ExifReader reader;
            try
            {
                reader = new ExifReader(seekable);
            }
            catch (ExifLibException)
            {
                return new ExifAltitudeResult(false, null);
            }

            using (reader)
            {
                double altitudeMeters;
                bool hasAltitude = false;

                if (reader.GetTagValue(ExifTags.GPSAltitude, out double[] altDoubles) && altDoubles != null && altDoubles.Length > 0)
                {
                    altitudeMeters = altDoubles[0];
                    hasAltitude = true;
                }
                else if (reader.GetTagValue(ExifTags.GPSAltitude, out uint[] altRational) && altRational != null && altRational.Length >= 2 && altRational[1] != 0)
                {
                    altitudeMeters = altRational[0] / (double)altRational[1];
                    hasAltitude = true;
                }
                else
                {
                    altitudeMeters = 0;
                }

                if (!hasAltitude)
                    return new ExifAltitudeResult(true, null);

                if (reader.GetTagValue(ExifTags.GPSAltitudeRef, out byte altRef) && altRef == 1)
                    altitudeMeters = -altitudeMeters;

                return new ExifAltitudeResult(true, altitudeMeters);
            }
        }
        finally
        {
            buffered?.Dispose();
        }
    }

    private static IFileStorage CreateFileStorage(string storageType, string connection)
    {
        if (storageType == "fs")
        {
            return new SystemIoFileStorage(
                path => path,
                Options.Create(new SystemIoStorageOptions() { PathTemplate = connection }),
                ImageFormatDefinition.Jpeg);
        }

        if (storageType == "azure")
            return new AzureFileStorage(Options.Create(new AzureStorageOptions() { ConnectionString = connection }));

        Console.WriteLine($"Not supported type of file storage '{storageType}'.");
        return null;
    }

    private static bool TryGetConfiguration(string[] args, out string connectionString, out string storageType, out string storageConnectionString, out bool isDryRun)
    {
        connectionString = null;
        storageType = null;
        storageConnectionString = null;
        isDryRun = false;

        if (args.Length == 0)
        {
            PrintUsage();
            return false;
        }

        var positional = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--dry-run")
            {
                isDryRun = true;
            }
            else if (arg == "--storage")
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("Missing value for --storage.");
                    return false;
                }
                storageType = args[++i];
            }
            else if (arg == "--storage-conn")
            {
                if (i + 1 >= args.Length)
                {
                    Console.WriteLine("Missing value for --storage-conn.");
                    return false;
                }
                storageConnectionString = args[++i];
            }
            else
            {
                positional.Add(arg);
            }
        }

        if (positional.Count != 1)
        {
            PrintUsage();
            return false;
        }

        if ((storageType == null) != (storageConnectionString == null))
        {
            Console.WriteLine("--storage and --storage-conn must be provided together.");
            return false;
        }

        connectionString = positional[0];
        return true;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: dotnet run ./src/AltitudeBackfill.cs -- <db-connection-string> [--storage fs|azure --storage-conn <value>] [--dry-run]");
        Console.WriteLine();
        Console.WriteLine("Unsets altitudes that fall outside plausible Earth bounds");
        Console.WriteLine($"  (min = {AltitudeBounds.MinMeters} m Challenger Deep, max = {AltitudeBounds.MaxMeters} m Mount Everest)");
        Console.WriteLine("across Image/Video locations, Entry ordered locations and Entry.TrackAltitude.");
        Console.WriteLine("When --storage is provided, image originals are read and their EXIF altitude is compared,");
        Console.WriteLine("flagging entries whose stored value disagrees with what the photo itself contains.");
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
