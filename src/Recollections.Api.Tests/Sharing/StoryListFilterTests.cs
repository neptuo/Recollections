using System.Net;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Tests.Infrastructure;
using Neptuo.Recollections.Sharing;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class StoryListFilterTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "sl-usera-id";
    private const string UserAName = "slusera";
    private const string UserBId = "sl-userb-id";
    private const string UserBName = "sluserb";
    private const string UserCId = "sl-userc-id";
    private const string UserCName = "sluserc";

    private const string StoryOwnedByAInherited = "sl-story-a-inherited";
    private const string StorySharedWithB = "sl-story-a-shared";
    private const string StoryPrivateA = "sl-story-a-private";
    private const string StoryOwnedByBInherited = "sl-story-b-inherited";
    private const string StoryOwnedByCPrivate = "sl-story-c-private";
    private const string EntrySharedWithBVisible = "sl-entry-a-shared-visible";
    private const string EntrySharedWithBHidden = "sl-entry-a-shared-hidden";

    public StoryListFilterTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(StoryListFilterTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedUser(accountsDb, UserCId, UserCName);

            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            var storyAInherited = await DatabaseSeeder.SeedStory(entriesDb, StoryOwnedByAInherited, UserAId, isSharingInherited: true);
            var storyAShared = await DatabaseSeeder.SeedStory(entriesDb, StorySharedWithB, UserAId, isSharingInherited: false);
            var storyAPrivate = await DatabaseSeeder.SeedStory(entriesDb, StoryPrivateA, UserAId, isSharingInherited: false);
            var storyBInherited = await DatabaseSeeder.SeedStory(entriesDb, StoryOwnedByBInherited, UserBId, isSharingInherited: true);
            var storyCPrivate = await DatabaseSeeder.SeedStory(entriesDb, StoryOwnedByCPrivate, UserCId, isSharingInherited: false);

            await DatabaseSeeder.SeedStoryShare(entriesDb, StorySharedWithB, UserBId, Permission.Read);

            await DatabaseSeeder.SeedEntry(entriesDb, "sl-entry-a-inherited", UserAId, isSharingInherited: true, story: storyAInherited);
            await DatabaseSeeder.SeedEntry(entriesDb, EntrySharedWithBVisible, UserAId, isSharingInherited: true, story: storyAShared);
            await DatabaseSeeder.SeedEntry(entriesDb, EntrySharedWithBHidden, UserAId, isSharingInherited: false, story: storyAShared);
            await DatabaseSeeder.SeedEntry(entriesDb, "sl-entry-a-private", UserAId, isSharingInherited: true, story: storyAPrivate);
            await DatabaseSeeder.SeedEntry(entriesDb, "sl-entry-b-inherited", UserBId, isSharingInherited: true, story: storyBInherited);
            await DatabaseSeeder.SeedEntry(entriesDb, "sl-entry-c-private", UserCId, isSharingInherited: true, story: storyCPrivate);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<List<StoryListModel>> GetStoriesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/stories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<List<StoryListModel>>();
    }

    [Fact]
    public async Task StoryList_UserA_ReturnsOwnAndConnectedStoriesOnly()
    {
        var client = factory.CreateClientForUser(UserAId, UserAName);
        var models = await GetStoriesAsync(client);
        var storyIds = models.Select(m => m.Id).ToList();
        var modelById = models.ToDictionary(m => m.Id);

        Assert.Contains(StoryOwnedByAInherited, storyIds);
        Assert.Contains(StorySharedWithB, storyIds);
        Assert.Contains(StoryPrivateA, storyIds);
        Assert.Contains(StoryOwnedByBInherited, storyIds);

        Assert.DoesNotContain(StoryOwnedByCPrivate, storyIds);
        Assert.Equal(4, models.Count);
        Assert.Equal(1, modelById[StoryOwnedByAInherited].Entries);
        Assert.Equal(2, modelById[StorySharedWithB].Entries);
        Assert.Equal(1, modelById[StoryPrivateA].Entries);
        Assert.Equal(1, modelById[StoryOwnedByBInherited].Entries);
    }

    [Fact]
    public async Task StoryList_UserB_ReturnsOwnAndAccessibleStoriesOnly()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var models = await GetStoriesAsync(client);
        var storyIds = models.Select(m => m.Id).ToList();
        var modelById = models.ToDictionary(m => m.Id);

        Assert.Contains(StoryOwnedByBInherited, storyIds);
        Assert.Contains(StoryOwnedByAInherited, storyIds);
        Assert.Contains(StorySharedWithB, storyIds);

        Assert.DoesNotContain(StoryPrivateA, storyIds);
        Assert.DoesNotContain(StoryOwnedByCPrivate, storyIds);
        Assert.Equal(3, models.Count);
        Assert.Equal(1, modelById[StoryOwnedByBInherited].Entries);
        Assert.Equal(1, modelById[StoryOwnedByAInherited].Entries);
        Assert.Equal(1, modelById[StorySharedWithB].Entries);
    }

    [Fact]
    public async Task StoryList_UserC_ReturnsOnlyOwnStories()
    {
        var client = factory.CreateClientForUser(UserCId, UserCName);
        var models = await GetStoriesAsync(client);
        var storyIds = models.Select(m => m.Id).ToList();
        var modelById = models.ToDictionary(m => m.Id);

        Assert.Contains(StoryOwnedByCPrivate, storyIds);

        Assert.DoesNotContain(StoryOwnedByAInherited, storyIds);
        Assert.DoesNotContain(StorySharedWithB, storyIds);
        Assert.DoesNotContain(StoryPrivateA, storyIds);
        Assert.DoesNotContain(StoryOwnedByBInherited, storyIds);
        Assert.Single(models);
        Assert.Equal(1, modelById[StoryOwnedByCPrivate].Entries);
    }

    [Fact]
    public async Task StoryList_Anonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/stories");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
