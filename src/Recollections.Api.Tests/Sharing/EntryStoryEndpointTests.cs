using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class EntryStoryEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "es-owner-id";
    private const string OwnerUserName = "esowner";
    private const string ReaderUserId = "es-reader-id";
    private const string ReaderUserName = "esreader";
    private const string StrangerUserId = "es-stranger-id";
    private const string StrangerUserName = "esstranger";

    private const string StoryId = "es-story-id";
    private const string StoryTitle = "Story es-story-id";
    private const string ChapterId = "es-chapter-id";
    private const string ChapterTitle = "Chapter es-chapter-id";

    private const string SharedEntryId = "es-entry-shared";
    private const string PrivateEntryId = "es-entry-private";
    private const string PublicEntryId = "es-entry-public";
    private const string PublicChapterEntryId = "es-entry-public-chapter";

    public EntryStoryEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryStoryEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId,
                permission1: Permission.Read, permission2: Permission.Read);

            var story = await DatabaseSeeder.SeedStory(entriesDb, StoryId, OwnerUserId, isSharingInherited: false);
            story.Title = StoryTitle;
            var chapter = await DatabaseSeeder.SeedChapter(entriesDb, ChapterId, story, ChapterTitle);
            await entriesDb.SaveChangesAsync();

            // Shared entry (explicit share to reader) with story
            await DatabaseSeeder.SeedEntry(entriesDb, SharedEntryId, OwnerUserId, isSharingInherited: false, story: story);
            await DatabaseSeeder.SeedEntryShare(entriesDb, SharedEntryId, ReaderUserId, Permission.Read);

            // Private entry (no reader share) with story
            await DatabaseSeeder.SeedEntry(entriesDb, PrivateEntryId, OwnerUserId, isSharingInherited: false, story: story);

            // Public entry attached directly to story
            await DatabaseSeeder.SeedEntry(entriesDb, PublicEntryId, OwnerUserId, isSharingInherited: false, story: story);
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicEntryId, ShareStatusService.PublicUserId, Permission.Read);

            // Public entry attached via chapter
            await DatabaseSeeder.SeedEntry(entriesDb, PublicChapterEntryId, OwnerUserId, isSharingInherited: false, chapter: chapter);
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicChapterEntryId, ShareStatusService.PublicUserId, Permission.Read);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SharedEntry_AsOwner_ReturnsStoryMetadata()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/story");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.ReadJsonAsync<EntryStoryModel>();
        Assert.Equal(StoryId, model.StoryId);
        Assert.Equal(StoryTitle, model.StoryTitle);
        Assert.Null(model.ChapterId);
    }

    [Fact]
    public async Task SharedEntry_AsExplicitReader_ReturnsStoryMetadata()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/story");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.ReadJsonAsync<EntryStoryModel>();
        Assert.Equal(StoryId, model.StoryId);
    }

    [Fact]
    public async Task SharedEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/story");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SharedEntry_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{SharedEntryId}/story");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PrivateEntry_AsExplicitReader_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/story");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicEntry_AsAnonymous_ReturnsStoryMetadata()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/story");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.ReadJsonAsync<EntryStoryModel>();
        Assert.Equal(StoryId, model.StoryId);
        Assert.Equal(StoryTitle, model.StoryTitle);
        Assert.Null(model.ChapterId);
    }

    [Fact]
    public async Task PublicChapterEntry_AsAnonymous_ReturnsChapterMetadata()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicChapterEntryId}/story");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var model = await response.ReadJsonAsync<EntryStoryModel>();
        Assert.Equal(StoryId, model.StoryId);
        Assert.Equal(ChapterId, model.ChapterId);
        Assert.Equal(ChapterTitle, model.ChapterTitle);
    }
}
