using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Tests.Entries;

/// <summary>
/// Validates that image upload with EXIF GPS data correctly sanitizes coordinates through the full import pipeline.
/// Uses synthetic fixture to avoid shipping personal data.
/// </summary>
[Collection(nameof(TestFixtureCollection))]
public class ImageImportRegressionTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "image-import-owner-id";
    private const string OwnerUserName = "imageimportowner";
    private const string EntryId = "image-import-entry-id";

    public ImageImportRegressionTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(ImageImportRegressionTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedEntry(entriesDb, EntryId, OwnerUserId, isSharingInherited: true);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_WithSyntheticExifImage_SucceedsAndStoresFiniteCoordinates()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);

        var response = await client.PostAsync($"/api/entries/{EntryId}/media", CreateImageUpload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var media = await response.ReadJsonAsync<MediaModel>();
        Assert.Equal("image", media.Type);

        var model = media.Image;
        Assert.NotNull(model);
        Assert.NotNull(model.Location.Latitude);
        Assert.NotNull(model.Location.Longitude);
        Assert.Equal(10.5d, model.Location.Latitude.Value, precision: 2);
        Assert.Equal(20.75d, model.Location.Longitude.Value, precision: 2);
        Assert.True(double.IsFinite(model.Location.Latitude.Value));
        Assert.True(double.IsFinite(model.Location.Longitude.Value));
        Assert.True(model.Location.Altitude == null || AltitudeBounds.IsValid(model.Location.Altitude));

        using var scope = factory.Services.CreateScope();
        var entriesDb = scope.ServiceProvider.GetRequiredService<EntriesDataContext>();
        var entity = await entriesDb.Images.SingleAsync(i => i.Entry.Id == EntryId && i.Id == model.Id);
        Assert.Equal(model.Location.Latitude, entity.Location?.Latitude);
        Assert.Equal(model.Location.Longitude, entity.Location?.Longitude);
        Assert.Equal(model.Location.Altitude, entity.Location?.Altitude);
    }

    private static MultipartFormDataContent CreateImageUpload()
    {
        var content = new MultipartFormDataContent();
        var file = new StreamContent(File.OpenRead(GetSyntheticImagePath()));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "synthetic-exif-gps.jpg");
        return content;
    }

    private static string GetSyntheticImagePath()
        => Path.Combine(AppContext.BaseDirectory, "TestData", "Images", "synthetic-exif-gps.jpg");
}
