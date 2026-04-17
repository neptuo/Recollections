using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

/// <summary>
/// Covers the authorization gate for video detail and video file routes.
/// File content routes return <c>404 NotFound</c> when auth passes because no real files
/// are seeded on disk — that is enough to prove the authorization behavior.
/// </summary>
public class EntryVideoDetailAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "evd-owner-id";
    private const string OwnerUserName = "evdowner";
    private const string ReaderUserId = "evd-reader-id";
    private const string ReaderUserName = "evdreader";
    private const string StrangerUserId = "evd-stranger-id";
    private const string StrangerUserName = "evdstranger";

    private const string PrivateEntryId = "evd-entry-private";
    private const string SharedEntryId = "evd-entry-shared";
    private const string PublicEntryId = "evd-entry-public";

    private const string PrivateVideoId = "evd-video-private";
    private const string SharedVideoId = "evd-video-shared";
    private const string PublicVideoId = "evd-video-public";

    public EntryVideoDetailAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryVideoDetailAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId,
                permission1: Permission.Read, permission2: Permission.Read);

            var privateEntry = await DatabaseSeeder.SeedEntry(entriesDb, PrivateEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedVideo(entriesDb, PrivateVideoId, privateEntry);

            var sharedEntry = await DatabaseSeeder.SeedEntry(entriesDb, SharedEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, SharedEntryId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedVideo(entriesDb, SharedVideoId, sharedEntry);

            var publicEntry = await DatabaseSeeder.SeedEntry(entriesDb, PublicEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicEntryId, ShareStatusService.PublicUserId, Permission.Read);
            await DatabaseSeeder.SeedVideo(entriesDb, PublicVideoId, publicEntry);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== Detail =====

    [Fact]
    public async Task Detail_SharedEntry_AsOwner_ReturnsOk()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/videos/{SharedVideoId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<VideoModel>>();
        Assert.Equal(SharedVideoId, result.Model.Id);
        Assert.Equal(OwnerUserId, result.OwnerId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task Detail_SharedEntry_AsExplicitReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/videos/{SharedVideoId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<VideoModel>>();
        Assert.Equal(SharedVideoId, result.Model.Id);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task Detail_PrivateEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/videos/{PrivateVideoId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Detail_PrivateEntry_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/videos/{PrivateVideoId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Detail_PublicEntry_AsAnonymous_ReturnsReadPermission()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/videos/{PublicVideoId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<VideoModel>>();
        Assert.Equal(PublicVideoId, result.Model.Id);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    // ===== File routes =====

    public static IEnumerable<object[]> FileRoutes()
    {
        yield return new object[] { "thumbnail" };
        yield return new object[] { "preview" };
        yield return new object[] { "original" };
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_PrivateEntry_AsStranger_ReturnsUnauthorized(string route)
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/videos/{PrivateVideoId}/{route}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_PrivateEntry_AsAnonymous_ReturnsUnauthorized(string route)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/videos/{PrivateVideoId}/{route}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_PublicEntry_AsAnonymous_AuthorizationGatePasses(string route)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/videos/{PublicVideoId}/{route}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_SharedEntry_AsExplicitReader_AuthorizationGatePasses(string route)
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/videos/{SharedVideoId}/{route}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory, MemberData(nameof(FileRoutes))]
    public async Task File_SharedEntry_AsOwner_AuthorizationGatePasses(string route)
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/videos/{SharedVideoId}/{route}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
