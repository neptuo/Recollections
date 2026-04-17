using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

/// <summary>
/// Covers the authorization gate for image detail and image file routes.
/// File content routes return <c>404 NotFound</c> when auth passes because no real files
/// are seeded on disk — that is enough to prove the authorization behavior.
/// </summary>
public class EntryImageDetailAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "eid-owner-id";
    private const string OwnerUserName = "eidowner";
    private const string ReaderUserId = "eid-reader-id";
    private const string ReaderUserName = "eidreader";
    private const string StrangerUserId = "eid-stranger-id";
    private const string StrangerUserName = "eidstranger";

    private const string PrivateEntryId = "eid-entry-private";
    private const string SharedEntryId = "eid-entry-shared";
    private const string PublicEntryId = "eid-entry-public";

    private const string PrivateImageId = "eid-image-private";
    private const string SharedImageId = "eid-image-shared";
    private const string PublicImageId = "eid-image-public";

    public EntryImageDetailAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryImageDetailAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId,
                permission1: Permission.Read, permission2: Permission.Read);

            var privateEntry = await DatabaseSeeder.SeedEntry(entriesDb, PrivateEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedImage(entriesDb, PrivateImageId, privateEntry);

            var sharedEntry = await DatabaseSeeder.SeedEntry(entriesDb, SharedEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, SharedEntryId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedImage(entriesDb, SharedImageId, sharedEntry);

            var publicEntry = await DatabaseSeeder.SeedEntry(entriesDb, PublicEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicEntryId, ShareStatusService.PublicUserId, Permission.Read);
            await DatabaseSeeder.SeedImage(entriesDb, PublicImageId, publicEntry);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== Detail =====

    [Fact]
    public async Task Detail_SharedEntry_AsOwner_ReturnsOk()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/images/{SharedImageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<ImageModel>>();
        Assert.Equal(SharedImageId, result.Model.Id);
        Assert.Equal(OwnerUserId, result.OwnerId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task Detail_SharedEntry_AsExplicitReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/images/{SharedImageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<ImageModel>>();
        Assert.Equal(SharedImageId, result.Model.Id);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task Detail_PrivateEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/images/{PrivateImageId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Detail_PrivateEntry_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/images/{PrivateImageId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Detail_PublicEntry_AsAnonymous_ReturnsReadPermission()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/images/{PublicImageId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<ImageModel>>();
        Assert.Equal(PublicImageId, result.Model.Id);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    // ===== File routes (preview, thumbnail, original) =====

    public static IEnumerable<object[]> FileRoutes()
    {
        yield return new object[] { "preview" };
        yield return new object[] { "thumbnail" };
        yield return new object[] { "original" };
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_PrivateEntry_AsStranger_ReturnsUnauthorized(string route)
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/images/{PrivateImageId}/{route}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_PrivateEntry_AsAnonymous_ReturnsUnauthorized(string route)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/images/{PrivateImageId}/{route}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_PublicEntry_AsAnonymous_AuthorizationGatePasses(string route)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/images/{PublicImageId}/{route}");

        // Auth passed → controller reaches file storage, which has no file → 404.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_SharedEntry_AsExplicitReader_AuthorizationGatePasses(string route)
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/images/{SharedImageId}/{route}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_SharedEntry_AsOwner_AuthorizationGatePasses(string route)
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/images/{SharedImageId}/{route}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
