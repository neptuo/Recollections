using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class StoryChaptersAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "sc-owner-id";
    private const string OwnerUserName = "scowner";
    private const string ReaderUserId = "sc-reader-id";
    private const string ReaderUserName = "screader";
    private const string StrangerUserId = "sc-stranger-id";
    private const string StrangerUserName = "scstranger";

    private const string SharedStoryId = "sc-story-shared";
    private const string PrivateStoryId = "sc-story-private";
    private const string PublicStoryId = "sc-story-public";

    private const string SharedChapterAId = "sc-shared-chapter-a";
    private const string SharedChapterBId = "sc-shared-chapter-b";
    private const string PrivateChapterId = "sc-private-chapter";
    private const string PublicChapterId = "sc-public-chapter";

    public StoryChaptersAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(StoryChaptersAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            var sharedStory = await DatabaseSeeder.SeedStory(entriesDb, SharedStoryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, SharedStoryId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedChapter(entriesDb, SharedChapterAId, sharedStory, title: "Shared Chapter A", order: 1);
            await DatabaseSeeder.SeedChapter(entriesDb, SharedChapterBId, sharedStory, title: "Shared Chapter B", order: 2);

            var privateStory = await DatabaseSeeder.SeedStory(entriesDb, PrivateStoryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedChapter(entriesDb, PrivateChapterId, privateStory, title: "Private Chapter");

            var publicStory = await DatabaseSeeder.SeedStory(entriesDb, PublicStoryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, PublicStoryId, ShareStatusService.PublicUserId, Permission.Read);
            await DatabaseSeeder.SeedChapter(entriesDb, PublicChapterId, publicStory, title: "Public Chapter");
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SharedStoryChapters_AsOwner_ReturnsAllChapters()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/stories/{SharedStoryId}/chapters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<StoryChapterListModel>>();
        var chapterIds = models.Select(m => m.Id).ToList();
        Assert.Equal(2, models.Count);
        Assert.Contains(SharedChapterAId, chapterIds);
        Assert.Contains(SharedChapterBId, chapterIds);
    }

    [Fact]
    public async Task SharedStoryChapters_AsExplicitReader_ReturnsAllChapters()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/stories/{SharedStoryId}/chapters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<StoryChapterListModel>>();
        Assert.Equal(2, models.Count);
    }

    [Fact]
    public async Task PrivateStoryChapters_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/stories/{PrivateStoryId}/chapters");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PrivateStoryChapters_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/stories/{PrivateStoryId}/chapters");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SharedStoryChapters_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/stories/{SharedStoryId}/chapters");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicStoryChapters_AsAnonymous_ReturnsChapters()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/stories/{PublicStoryId}/chapters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<StoryChapterListModel>>();
        Assert.Single(models);
        Assert.Equal(PublicChapterId, models[0].Id);
    }
}
