using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class EntryTrackImportTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "track-owner-id";
    private const string OwnerUserName = "trackowner";
    private const string ReaderUserId = "track-reader-id";
    private const string ReaderUserName = "trackreader";
    private const string CoOwnerUserId = "track-coowner-id";
    private const string CoOwnerUserName = "trackcoowner";

    private const string EntryId = "track-entry-id";

    public EntryTrackImportTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryTrackImportTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, CoOwnerUserId, CoOwnerUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId, Permission.Read, Permission.Read);
            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, CoOwnerUserId, Permission.CoOwner, Permission.Read);

            var entry = await DatabaseSeeder.SeedEntry(entriesDb, EntryId, OwnerUserId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, entry, 48.100, 17.100);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TrackImport_AsCoOwner_ReplacesEntryTrackData()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);

        var response = await client.PostAsync($"/api/entries/{EntryId}/track", CreateTrackUpload(CreateGpx([
            (50.087, 14.421, 210d),
            (50.095, 14.430, 215d),
            (50.103, 14.438, 220d),
            (50.111, 14.446, 225d)
        ])));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response = await client.PostAsync($"/api/entries/{EntryId}/track", CreateTrackUpload(CreateGpx([
            (49.195, 16.608, 230d),
            (49.205, 16.618, 235d),
            (49.215, 16.628, 240d)
        ])));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.ReadJsonAsync<EntryModel>();
        Assert.Single(model.Locations);
        Assert.Equal(48.100, model.Locations[0].Latitude);
        Assert.Equal(17.100, model.Locations[0].Longitude);
        Assert.True(model.Track.HasValue());
        Assert.Equal(3, model.Track.PointCount);
        Assert.Equal(10d, model.Track.TotalElevation);
        Assert.Equal(49.205, model.Track.Location.Latitude);
        Assert.Equal(16.618, model.Track.Location.Longitude);

        var decoded = new GpxImportService().Decode(model.Track.Data);
        Assert.Equal(3, decoded.Count);
        Assert.Equal(49.195, decoded[0].Latitude);
        Assert.Equal(16.628, decoded[2].Longitude);

        var entryResponse = await client.GetAsync($"/api/entries/{EntryId}");
        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);
        var entry = await entryResponse.ReadJsonAsync<AuthorizedModel<EntryModel>>();
        Assert.True(entry.Model.Track.HasValue());
        Assert.Single(entry.Model.Locations);
        Assert.Equal(3, entry.Model.Track.PointCount);
        Assert.Equal(10d, entry.Model.Track.TotalElevation);
    }

    [Fact]
    public async Task TrackImport_AsReader_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.PostAsync($"/api/entries/{EntryId}/track", CreateTrackUpload(CreateGpx([
            (50.087, 14.421, 210d),
            (50.095, 14.430, 215d)
        ])));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TrackImport_InvalidFile_ReturnsBadRequest()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var response = await client.PostAsync($"/api/entries/{EntryId}/track", CreateTrackUpload("<gpx></gpx>"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void TrackImport_InvalidStream_ThrowsValidationException()
    {
        var service = new GpxImportService();

        Assert.Throws<TrackImportValidationException>(() => service.Parse(new ThrowingFileInput(new IOException("Broken upload stream."))));
    }

    [Fact]
    public void TrackImport_MissingCoordinates_ThrowsValidationException()
    {
        var service = new GpxImportService();

        Assert.Throws<TrackImportValidationException>(() => service.Create([
            new LocationModel() { Latitude = 50.087, Longitude = 14.421 },
            new LocationModel() { Latitude = 50.095 }
        ]));
    }

    [Fact]
    public void TrackImport_ComputesTotalElevationGain()
    {
        var service = new GpxImportService();

        var track = service.Create([
            new LocationModel() { Latitude = 50.087, Longitude = 14.421, Altitude = 100d },
            new LocationModel() { Latitude = 50.095, Longitude = 14.430, Altitude = 120d },
            new LocationModel() { Latitude = 50.103, Longitude = 14.438, Altitude = 110d },
            new LocationModel() { Latitude = 50.111, Longitude = 14.446, Altitude = 150d }
        ]);

        Assert.Equal(60d, track.TotalElevation);
    }

    [Fact]
    public async Task TrackImport_LargeTrack_IsSimplified()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var response = await client.PostAsync(
            $"/api/entries/{EntryId}/track",
            CreateTrackUpload(CreateLargeGpx(GpxImportService.MaxLocationCount + 75))
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.ReadJsonAsync<EntryModel>();
        Assert.True(model.Track.HasValue());
        Assert.Equal(GpxImportService.MaxLocationCount, model.Track.PointCount);

        var decoded = new GpxImportService().Decode(model.Track.Data);
        Assert.Equal(GpxImportService.MaxLocationCount, decoded.Count);
        Assert.Equal(50d, decoded[0].Latitude);
        Assert.Equal(14d, decoded[0].Longitude);
        Assert.Equal(50d + ((GpxImportService.MaxLocationCount + 75 - 1) * 0.001d), decoded[^1].Latitude);
    }

    [Fact]
    public async Task TrackImport_CanBeClearedByEntryUpdate()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var importResponse = await client.PostAsync($"/api/entries/{EntryId}/track", CreateTrackUpload(CreateGpx([
            (50.087, 14.421, 210d),
            (50.095, 14.430, 215d)
        ])));
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/entries/{EntryId}");
        var entry = await getResponse.ReadJsonAsync<AuthorizedModel<EntryModel>>();

        var json = $"{{\"Id\":\"{EntryId}\",\"Title\":\"{entry.Model.Title}\",\"When\":\"{entry.Model.When:O}\",\"Text\":null,\"Locations\":[],\"Track\":{{\"Data\":null,\"PointCount\":0,\"TotalElevation\":null,\"Location\":null}}}}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var updateResponse = await client.PutAsync($"/api/entries/{EntryId}", content);

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        getResponse = await client.GetAsync($"/api/entries/{EntryId}");
        entry = await getResponse.ReadJsonAsync<AuthorizedModel<EntryModel>>();
        Assert.False(entry.Model.Track.HasValue());
    }

    private static MultipartFormDataContent CreateTrackUpload(string gpx, string fileName = "track.gpx")
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(gpx));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/gpx+xml");
        content.Add(file, "file", fileName);
        return content;
    }

    private static string CreateLargeGpx(int count)
    {
        var points = Enumerable.Range(0, count)
            .Select(i => (50d + (i * 0.001d), 14d + (i * 0.001d), 200d + i))
            .ToArray();

        return CreateGpx(points);
    }

    private static string CreateGpx((double latitude, double longitude, double altitude)[] points)
    {
        var track = new StringBuilder();
        foreach (var point in points)
        {
            track.AppendLine(
                $"<trkpt lat=\"{point.latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" lon=\"{point.longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}\"><ele>{point.altitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}</ele></trkpt>"
            );
        }

        return $$"""
            <?xml version="1.0" encoding="UTF-8"?>
            <gpx version="1.1" creator="tests" xmlns="http://www.topografix.com/GPX/1/1">
              <trk>
                <name>Sample track</name>
                <trkseg>
            {{track}}
                </trkseg>
              </trk>
            </gpx>
            """;
    }

    private sealed class ThrowingFileInput(Exception exception) : IFileInput
    {
        public string ContentType => "application/gpx+xml";
        public string FileName => "track.gpx";
        public long Length => 0;

        public Stream OpenReadStream() => throw exception;
    }
}
