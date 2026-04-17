using System.Net;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

/// <summary>
/// Covers the sharing/visibility behavior for <c>GET /api/profiles/{id}</c> and
/// additional anonymous/stranger/public coverage for
/// <c>GET /api/profiles/{id}/timeline/list</c>.
/// </summary>
public class ProfileAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string PublicProfileUserId = "pa-public-user-id";
    private const string PublicProfileUserName = "papublicuser";
    private const string PrivateProfileUserId = "pa-private-user-id";
    private const string PrivateProfileUserName = "paprivateuser";
    private const string ReaderUserId = "pa-reader-id";
    private const string ReaderUserName = "pareader";
    private const string StrangerUserId = "pa-stranger-id";
    private const string StrangerUserName = "pastranger";

    private const string PublicEntryId = "pa-entry-public";
    private const string PrivateEntryId = "pa-entry-private";

    public ProfileAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(ProfileAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, PublicProfileUserId, PublicProfileUserName);
            await DatabaseSeeder.SeedUser(accountsDb, PrivateProfileUserId, PrivateProfileUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, PublicProfileUserId, ReaderUserId,
                permission1: Permission.Read, permission2: Permission.Read);

            // Public profile: user being shared to PublicUserId
            await DatabaseSeeder.SeedUserBeing(entriesDb, PublicProfileUserId, PublicProfileUserName);
            await DatabaseSeeder.SeedBeingShare(entriesDb, PublicProfileUserId, ShareStatusService.PublicUserId, Permission.Read);
            await DatabaseSeeder.SeedBeingShare(entriesDb, PublicProfileUserId, ReaderUserId, Permission.Read);

            // Private profile: user being exists but is not shared to anyone
            await DatabaseSeeder.SeedUserBeing(entriesDb, PrivateProfileUserId, PrivateProfileUserName);

            // Entries on the public profile
            await DatabaseSeeder.SeedEntry(entriesDb, PublicEntryId, PublicProfileUserId, isSharingInherited: false,
                when: new DateTime(2024, 11, 1, 10, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicEntryId, ShareStatusService.PublicUserId, Permission.Read);

            await DatabaseSeeder.SeedEntry(entriesDb, PrivateEntryId, PublicProfileUserId, isSharingInherited: false,
                when: new DateTime(2024, 11, 2, 10, 0, 0, DateTimeKind.Utc));
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ===== GET /api/profiles/{id} =====

    [Fact]
    public async Task Profile_AsOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(PublicProfileUserId, PublicProfileUserName);
        var response = await client.GetAsync($"/api/profiles/{PublicProfileUserId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<ProfileModel>>();
        Assert.Equal(PublicProfileUserId, result.OwnerId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task Profile_AsConnectedReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/profiles/{PublicProfileUserId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<ProfileModel>>();
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task Profile_AsAnonymous_OnPublicProfile_ReturnsReadPermission()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/profiles/{PublicProfileUserId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadJsonAsync<AuthorizedModel<ProfileModel>>();
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task Profile_AsStranger_OnPrivateProfile_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/profiles/{PrivateProfileUserId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AsAnonymous_OnPrivateProfile_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/profiles/{PrivateProfileUserId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== GET /api/profiles/{id}/timeline/list =====

    [Fact]
    public async Task ProfileTimeline_AsStranger_OnPrivateProfile_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/profiles/{PrivateProfileUserId}/timeline/list?offset=0&count=20");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProfileTimeline_AsAnonymous_OnPrivateProfile_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/profiles/{PrivateProfileUserId}/timeline/list?offset=0&count=20");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProfileTimeline_AsAnonymous_OnPublicProfile_ReturnsOnlyPublicEntries()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/profiles/{PublicProfileUserId}/timeline/list?offset=0&count=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains(PublicEntryId, entryIds);
        Assert.DoesNotContain(PrivateEntryId, entryIds);
        Assert.Single(page.Models);
    }
}
