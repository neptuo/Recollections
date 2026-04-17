using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

/// <summary>
/// Covers anonymous / stranger / public-reader visibility for being-rooted read endpoints
/// that the existing suite only exercises as an authenticated connected reader:
/// /api/beings/{beingId}/timeline, /stories, /map.
/// </summary>
public class BeingVisibilityAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "bvi-owner-id";
    private const string OwnerUserName = "bviowner";
    private const string StrangerUserId = "bvi-stranger-id";
    private const string StrangerUserName = "bvistranger";

    private const string PrivateBeingId = "bvi-being-private";
    private const string PublicBeingId = "bvi-being-public";

    private const string PublicEntryId = "bvi-entry-public";
    private const string PrivateInheritedEntryId = "bvi-entry-private-inherited";
    private const string PublicStoryId = "bvi-story-public";

    public BeingVisibilityAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(BeingVisibilityAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            await DatabaseSeeder.SeedBeing(entriesDb, PrivateBeingId, OwnerUserId, isSharingInherited: false);

            var publicBeing = await DatabaseSeeder.SeedBeing(entriesDb, PublicBeingId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, PublicBeingId, ShareStatusService.PublicUserId, Permission.Read);

            var publicStory = await DatabaseSeeder.SeedStory(entriesDb, PublicStoryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, PublicStoryId, ShareStatusService.PublicUserId, Permission.Read);

            var publicEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                PublicEntryId,
                OwnerUserId,
                isSharingInherited: false,
                story: publicStory,
                when: new DateTime(2024, 10, 1, 10, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicEntryId, ShareStatusService.PublicUserId, Permission.Read);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, publicEntry, 48.2, 16.3);

            var inheritedEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                PrivateInheritedEntryId,
                OwnerUserId,
                isSharingInherited: true,
                when: new DateTime(2024, 10, 2, 10, 0, 0, DateTimeKind.Utc));

            publicEntry.Beings.Add(publicBeing);
            inheritedEntry.Beings.Add(publicBeing);

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static IEnumerable<object[]> PrivateBeingRoutes()
    {
        yield return new object[] { $"/api/beings/{PrivateBeingId}/timeline?offset=0&count=20" };
        yield return new object[] { $"/api/beings/{PrivateBeingId}/stories" };
        yield return new object[] { $"/api/beings/{PrivateBeingId}/map" };
    }

    public static IEnumerable<object[]> PublicBeingRoutes()
    {
        yield return new object[] { $"/api/beings/{PublicBeingId}/timeline?offset=0&count=20" };
        yield return new object[] { $"/api/beings/{PublicBeingId}/stories" };
        yield return new object[] { $"/api/beings/{PublicBeingId}/map" };
    }

    [Theory, MemberData(nameof(PrivateBeingRoutes))]
    public async Task PrivateBeing_AsStranger_ReturnsUnauthorized(string route)
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(PrivateBeingRoutes))]
    public async Task PrivateBeing_AsAnonymous_ReturnsUnauthorized(string route)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(PublicBeingRoutes))]
    public async Task PublicBeing_AsAnonymous_ReturnsOk(string route)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PublicBeing_Timeline_AsAnonymous_ReturnsEntriesTaggedWithPublicBeing()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/beings/{PublicBeingId}/timeline?offset=0&count=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        // Both tagged entries become anonymously accessible through the public being association.
        Assert.Contains(PublicEntryId, entryIds);
        Assert.Contains(PrivateInheritedEntryId, entryIds);
    }

    [Fact]
    public async Task PublicBeing_Stories_AsAnonymous_ReturnsOnlyStoriesFromAccessibleEntries()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/beings/{PublicBeingId}/stories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<StoryListModel>>();
        var storyIds = models.Select(m => m.Id).ToList();

        Assert.Contains(PublicStoryId, storyIds);
        Assert.Single(models);
    }
}
