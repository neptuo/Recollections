using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class StoryMediaEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "md-usera-id";
    private const string UserAName = "mdusera";
    private const string UserBId = "md-userb-id";
    private const string UserBName = "mduserb";

    private const string StoryId = "md-story-id";
    private const string ChapterId = "md-story-chapter-id";
    private const string VisibleDirectEntryId = "md-entry-direct-visible";
    private const string VisibleChapterEntryId = "md-entry-chapter-visible";
    private const string HiddenDirectEntryId = "md-entry-direct-hidden";
    private const string VisibleDirectImageId = "md-image-direct-visible";
    private const string VisibleChapterImageId = "md-image-chapter-visible";
    private const string HiddenDirectImageId = "md-image-direct-hidden";

    public StoryMediaEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(StoryMediaEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);

            var story = await DatabaseSeeder.SeedStory(entriesDb, StoryId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, StoryId, UserBId, Permission.Read);
            var chapter = await DatabaseSeeder.SeedChapter(entriesDb, ChapterId, story);

            var visibleDirectEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleDirectEntryId,
                UserAId,
                isSharingInherited: true,
                story: story,
                when: new DateTime(2024, 10, 1, 10, 0, 0, DateTimeKind.Utc));

            var visibleChapterEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleChapterEntryId,
                UserAId,
                isSharingInherited: true,
                chapter: chapter,
                when: new DateTime(2024, 10, 2, 10, 0, 0, DateTimeKind.Utc));

            var hiddenDirectEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenDirectEntryId,
                UserAId,
                isSharingInherited: false,
                story: story,
                when: new DateTime(2024, 10, 3, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedImage(entriesDb, VisibleDirectImageId, visibleDirectEntry);
            await DatabaseSeeder.SeedImage(entriesDb, VisibleChapterImageId, visibleChapterEntry);
            await DatabaseSeeder.SeedImage(entriesDb, HiddenDirectImageId, hiddenDirectEntry);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task StoryImages_UserB_ReturnsOnlyImagesFromVisibleEntriesInTheStory()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/stories/{StoryId}/images");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryImagesModel>>();
        var entryIds = models.Select(model => model.EntryId).ToList();
        var modelById = models.ToDictionary(model => model.EntryId);

        Assert.Contains(VisibleDirectEntryId, entryIds);
        Assert.Contains(VisibleChapterEntryId, entryIds);
        Assert.DoesNotContain(HiddenDirectEntryId, entryIds);
        Assert.Equal(2, models.Count);
        Assert.Equal(VisibleDirectImageId, Assert.Single(modelById[VisibleDirectEntryId].Images).Id);
        Assert.Equal(VisibleChapterImageId, Assert.Single(modelById[VisibleChapterEntryId].Images).Id);
    }

    [Fact]
    public async Task StoryMedia_UserB_ReturnsOnlyMediaFromVisibleEntriesInTheStory()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/stories/{StoryId}/media");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryMediaModel>>();
        var entryIds = models.Select(model => model.EntryId).ToList();
        var modelById = models.ToDictionary(model => model.EntryId);

        Assert.Contains(VisibleDirectEntryId, entryIds);
        Assert.Contains(VisibleChapterEntryId, entryIds);
        Assert.DoesNotContain(HiddenDirectEntryId, entryIds);
        Assert.Equal(2, models.Count);
        Assert.Collection(
            modelById[VisibleDirectEntryId].Media,
            media =>
            {
                Assert.Equal("image", media.Type);
                Assert.Equal(VisibleDirectImageId, media.Image?.Id);
            });
        Assert.Collection(
            modelById[VisibleChapterEntryId].Media,
            media =>
            {
                Assert.Equal("image", media.Type);
                Assert.Equal(VisibleChapterImageId, media.Image?.Id);
            });
    }
}
