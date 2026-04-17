using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

/// <summary>
/// Covers the visibility gate for entry-rooted media list endpoints:
/// <c>GET /api/entries/{entryId}/media</c> and <c>GET /api/entries/{entryId}/images</c>.
/// </summary>
public class EntryMediaAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "ema-owner-id";
    private const string OwnerUserName = "emaowner";
    private const string ReaderUserId = "ema-reader-id";
    private const string ReaderUserName = "emareader";
    private const string StrangerUserId = "ema-stranger-id";
    private const string StrangerUserName = "emastranger";

    private const string PrivateEntryId = "ema-entry-private";
    private const string SharedEntryId = "ema-entry-shared";
    private const string PublicEntryId = "ema-entry-public";

    private const string PrivateEntryImageId = "ema-image-private";
    private const string SharedEntryImageId = "ema-image-shared";
    private const string PublicEntryImageId = "ema-image-public";

    public EntryMediaAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryMediaAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId,
                permission1: Permission.Read, permission2: Permission.Read);

            var privateEntry = await DatabaseSeeder.SeedEntry(entriesDb, PrivateEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedImage(entriesDb, PrivateEntryImageId, privateEntry);

            var sharedEntry = await DatabaseSeeder.SeedEntry(entriesDb, SharedEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, SharedEntryId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedImage(entriesDb, SharedEntryImageId, sharedEntry);

            var publicEntry = await DatabaseSeeder.SeedEntry(entriesDb, PublicEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicEntryId, ShareStatusService.PublicUserId, Permission.Read);
            await DatabaseSeeder.SeedImage(entriesDb, PublicEntryImageId, publicEntry);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== /media =====

    [Fact]
    public async Task Media_PrivateEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/media");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Media_PrivateEntry_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/media");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Media_SharedEntry_AsOwner_ReturnsSharedImage()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/media");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<MediaModel>>();
        Assert.Single(models);
        Assert.Equal("image", models[0].Type);
        Assert.Equal(SharedEntryImageId, models[0].Image?.Id);
    }

    [Fact]
    public async Task Media_SharedEntry_AsExplicitReader_ReturnsSharedImage()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/media");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<MediaModel>>();
        Assert.Single(models);
        Assert.Equal(SharedEntryImageId, models[0].Image?.Id);
    }

    [Fact]
    public async Task Media_PublicEntry_AsAnonymous_ReturnsPublicImage()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/media");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<MediaModel>>();
        Assert.Single(models);
        Assert.Equal(PublicEntryImageId, models[0].Image?.Id);
    }

    // ===== /images =====

    [Fact]
    public async Task Images_PrivateEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/images");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Images_PrivateEntry_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/images");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Images_SharedEntry_AsExplicitReader_ReturnsSharedImage()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/images");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<ImageModel>>();
        Assert.Single(models);
        Assert.Equal(SharedEntryImageId, models[0].Id);
    }

    [Fact]
    public async Task Images_PublicEntry_AsAnonymous_ReturnsPublicImage()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/images");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<ImageModel>>();
        Assert.Single(models);
        Assert.Equal(PublicEntryImageId, models[0].Id);
    }

    [Fact]
    public async Task Images_PublicEntry_AsOwner_ReturnsPublicImage()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/images");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<ImageModel>>();
        Assert.Single(models);
        Assert.Equal(PublicEntryImageId, models[0].Id);
    }
}
