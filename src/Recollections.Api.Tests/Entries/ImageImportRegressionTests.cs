using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Tests.Entries;

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
    public async Task Create_WithRegressionImage_SucceedsAndStoresFiniteCoordinates()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);

        var response = await client.PostAsync($"/api/entries/{EntryId}/media", CreateImageUpload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var media = await response.ReadJsonAsync<MediaModel>();
        Assert.Equal("image", media.Type);

        var model = media.Image;
        Assert.NotNull(model);
        Assert.Equal(50.0111233d, model.Location.Latitude);
        Assert.Equal(14.6262635d, model.Location.Longitude);
        Assert.True(model.Location.Latitude is double latitude && double.IsFinite(latitude));
        Assert.True(model.Location.Longitude is double longitude && double.IsFinite(longitude));
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
        var file = new StreamContent(File.OpenRead(GetRegressionImagePath()));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "file", "20260423_073316.jpg");
        return content;
    }

    private static string GetRegressionImagePath()
        => Path.Combine(AppContext.BaseDirectory, "TestData", "Images", "20260423_073316.jpg");
}
