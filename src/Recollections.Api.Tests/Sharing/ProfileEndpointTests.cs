using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class ProfileMapEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "pmp-usera-id";
    private const string UserAName = "pmpusera";
    private const string UserBId = "pmp-userb-id";
    private const string UserBName = "pmpuserb";

    private const string VisibleEntryId = "pmp-entry-visible";
    private const string HiddenEntryId = "pmp-entry-hidden";
    private const string OwnedByBEntryId = "pmp-entry-owned-by-b";

    public ProfileMapEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(ProfileMapEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            await DatabaseSeeder.SeedUserBeing(entriesDb, UserAId, UserAName);
            await DatabaseSeeder.SeedBeingShare(entriesDb, UserAId, UserBId, Permission.Read);

            var visibleEntry = await DatabaseSeeder.SeedEntry(entriesDb, VisibleEntryId, UserAId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, visibleEntry, 50.087, 14.421);

            var hiddenEntry = await DatabaseSeeder.SeedEntry(entriesDb, HiddenEntryId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, hiddenEntry, 48.856, 2.352);

            var ownedByBEntry = await DatabaseSeeder.SeedEntry(entriesDb, OwnedByBEntryId, UserBId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, ownedByBEntry, 51.507, -0.128);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProfileMap_UserB_ReturnsOnlyAccessibleLocationsOfProfileOwner()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/profiles/{UserAId}/map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<MapEntryModel>>();
        var entryIds = models.Select(model => model.Entry.Id).ToList();

        Assert.Contains(VisibleEntryId, entryIds);
        Assert.DoesNotContain(HiddenEntryId, entryIds);
        Assert.DoesNotContain(OwnedByBEntryId, entryIds);
        Assert.Single(models);
    }
}

public class ProfileStoriesEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "pst-usera-id";
    private const string UserAName = "pstusera";
    private const string UserBId = "pst-userb-id";
    private const string UserBName = "pstuserb";

    private const string StoryId = "pst-story-id";
    private const string VisibleEntryId = "pst-entry-visible";
    private const string HiddenEntryId = "pst-entry-hidden";

    public ProfileStoriesEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(ProfileStoriesEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            await DatabaseSeeder.SeedUserBeing(entriesDb, UserAId, UserAName);
            await DatabaseSeeder.SeedBeingShare(entriesDb, UserAId, UserBId, Permission.Read);

            var story = await DatabaseSeeder.SeedStory(entriesDb, StoryId, UserAId, isSharingInherited: true);

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleEntryId,
                UserAId,
                isSharingInherited: true,
                story: story,
                when: new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenEntryId,
                UserAId,
                isSharingInherited: false,
                story: story,
                when: new DateTime(2024, 6, 2, 10, 0, 0, DateTimeKind.Utc));
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProfileStories_UserB_ReturnsOnlyStoriesWithAccessibleEntries()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/profiles/{UserAId}/stories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<StoryListModel>>();
        var storyIds = models.Select(m => m.Id).ToList();

        Assert.Contains(StoryId, storyIds);
        Assert.Single(models);
    }
}

public class ProfileHighestAltitudeEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "pha-usera-id";
    private const string UserAName = "phausera";
    private const string UserBId = "pha-userb-id";
    private const string UserBName = "phauserb";

    private const string VisibleEntryId = "pha-entry-visible";
    private const string HiddenEntryId = "pha-entry-hidden";

    public ProfileHighestAltitudeEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(ProfileHighestAltitudeEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            await DatabaseSeeder.SeedUserBeing(entriesDb, UserAId, UserAName);
            await DatabaseSeeder.SeedBeingShare(entriesDb, UserAId, UserBId, Permission.Read);

            var visibleEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleEntryId,
                UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, visibleEntry, 50.087, 14.421, altitude: 500);

            var hiddenEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenEntryId,
                UserAId,
                isSharingInherited: false,
                when: new DateTime(2024, 1, 2, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, hiddenEntry, 48.149, 17.107, altitude: 3000);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProfileHighestAltitude_UserB_ReturnsOnlyAccessibleAltitudeEntries()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/profiles/{UserAId}/highest-altitude");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryListModel>>();
        var entryIds = models.Select(model => model.Id).ToList();

        Assert.Contains(VisibleEntryId, entryIds);
        Assert.DoesNotContain(HiddenEntryId, entryIds);
        Assert.Single(models);

        var visibleModel = models.Single(m => m.Id == VisibleEntryId);
        Assert.Equal(500, visibleModel.Altitude);
    }
}
