using System.Net;
using Newtonsoft.Json.Linq;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class MapListEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "mp-list-usera-id";
    private const string UserAName = "mplistusera";
    private const string UserBId = "mp-list-userb-id";
    private const string UserBName = "mplistuserb";

    private const string VisibleEntryId = "mp-list-entry-visible";
    private const string ImageFallbackEntryId = "mp-list-entry-image-fallback";
    private const string HiddenEntryId = "mp-list-entry-hidden";
    private const string OwnEntryId = "mp-list-entry-own";
    private const string NoLocationEntryId = "mp-list-entry-no-location";
    private const string TrackFallbackEntryId = "mp-list-entry-track-fallback";

    public MapListEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(MapListEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            var visibleEntry = await DatabaseSeeder.SeedEntry(entriesDb, VisibleEntryId, UserAId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, visibleEntry, 50.087, 14.421);

            var imageFallbackEntry = await DatabaseSeeder.SeedEntry(entriesDb, ImageFallbackEntryId, UserAId, isSharingInherited: true);
            await DatabaseSeeder.SeedImage(entriesDb, "mp-list-image-visible", imageFallbackEntry, latitude: 49.195, longitude: 16.608);

            var hiddenEntry = await DatabaseSeeder.SeedEntry(entriesDb, HiddenEntryId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, hiddenEntry, 48.856, 2.352);

            var ownEntry = await DatabaseSeeder.SeedEntry(entriesDb, OwnEntryId, UserBId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, ownEntry, 51.507, -0.128);

            var trackFallbackEntry = await DatabaseSeeder.SeedEntry(entriesDb, TrackFallbackEntryId, UserAId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntryTrack(entriesDb, trackFallbackEntry,
                (50.010, 14.010, null),
                (50.020, 14.020, null),
                (50.030, 14.030, null));

            await DatabaseSeeder.SeedEntry(entriesDb, NoLocationEntryId, UserAId, isSharingInherited: true);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MapList_UserB_ReturnsOnlyAccessibleEntriesThatHaveResolvableLocations()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/map/list");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<MapEntryModel>>();
        var entryIds = models.Select(model => model.Id).ToList();

        Assert.Contains(VisibleEntryId, entryIds);
        Assert.Contains(ImageFallbackEntryId, entryIds);
        Assert.Contains(OwnEntryId, entryIds);
        Assert.Contains(TrackFallbackEntryId, entryIds);
        Assert.DoesNotContain(HiddenEntryId, entryIds);
        Assert.DoesNotContain(NoLocationEntryId, entryIds);
        Assert.Equal(4, models.Count);
        Assert.All(models, model => Assert.True(model.Location?.HasValue() == true));

        var trackFallback = models.Single(model => model.Id == TrackFallbackEntryId);
        Assert.Equal(50.02, trackFallback.Location.Latitude);
        Assert.Equal(14.02, trackFallback.Location.Longitude);
    }

    [Fact]
    public async Task MapCountries_UserB_ReturnsOnlyCountriesVisitedByAccessibleEntries()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/map/countries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        var features = (JArray)JObject.Parse(json)["features"]!;

        Assert.NotNull(features);
        Assert.Equal(2, features.Count);
    }
}

public class StoryMapEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "mp-story-usera-id";
    private const string UserAName = "mpstoryusera";
    private const string UserBId = "mp-story-userb-id";
    private const string UserBName = "mpstoryuserb";

    private const string StoryId = "mp-story-id";
    private const string ChapterId = "mp-story-chapter-id";
    private const string VisibleDirectEntryId = "mp-story-entry-direct-visible";
    private const string HiddenDirectEntryId = "mp-story-entry-direct-hidden";
    private const string VisibleChapterEntryId = "mp-story-entry-chapter-visible";

    public StoryMapEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(StoryMapEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);

            var story = await DatabaseSeeder.SeedStory(entriesDb, StoryId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, StoryId, UserBId, Permission.Read);
            var chapter = await DatabaseSeeder.SeedChapter(entriesDb, ChapterId, story);

            var visibleDirectEntry = await DatabaseSeeder.SeedEntry(entriesDb, VisibleDirectEntryId, UserAId, isSharingInherited: true, story: story);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, visibleDirectEntry, 40.7128, -74.0060);

            var hiddenDirectEntry = await DatabaseSeeder.SeedEntry(entriesDb, HiddenDirectEntryId, UserAId, isSharingInherited: false, story: story);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, hiddenDirectEntry, 34.0522, -118.2437);

            var visibleChapterEntry = await DatabaseSeeder.SeedEntry(entriesDb, VisibleChapterEntryId, UserAId, isSharingInherited: true, chapter: chapter);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, visibleChapterEntry, 41.9028, 12.4964);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task StoryMap_UserB_ReturnsOnlyVisibleLocationsFromThatStory()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/stories/{StoryId}/map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<MapEntryModel>>();
        var entryIds = models.Select(model => model.Id).ToList();

        Assert.Contains(VisibleDirectEntryId, entryIds);
        Assert.Contains(VisibleChapterEntryId, entryIds);
        Assert.DoesNotContain(HiddenDirectEntryId, entryIds);
        Assert.Equal(2, models.Count);
    }
}

public class BeingMapEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "mp-being-usera-id";
    private const string UserAName = "mpbeingusera";
    private const string UserBId = "mp-being-userb-id";
    private const string UserBName = "mpbeinguserb";

    private const string BeingId = "mp-being-id";
    private const string VisibleEntryId = "mp-being-entry-visible";
    private const string HiddenEntryId = "mp-being-entry-hidden";

    public BeingMapEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(BeingMapEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            var being = await DatabaseSeeder.SeedBeing(entriesDb, BeingId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, BeingId, UserBId, Permission.Read);

            var visibleEntry = await DatabaseSeeder.SeedEntry(entriesDb, VisibleEntryId, UserAId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, visibleEntry, 52.52, 13.405);

            var hiddenEntry = await DatabaseSeeder.SeedEntry(entriesDb, HiddenEntryId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, hiddenEntry, 59.3293, 18.0686);

            visibleEntry.Beings.Add(being);
            hiddenEntry.Beings.Add(being);

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BeingMap_UserB_ReturnsOnlyVisibleLocationsLinkedToTheBeing()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/beings/{BeingId}/map");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<MapEntryModel>>();
        var entryIds = models.Select(model => model.Id).ToList();

        Assert.Contains(VisibleEntryId, entryIds);
        Assert.DoesNotContain(HiddenEntryId, entryIds);
        Assert.Single(models);
    }
}
