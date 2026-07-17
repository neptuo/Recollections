using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class StorySearchEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "ss-search-usera-id";
    private const string UserAName = "sssearchusera";
    private const string UserBId = "ss-search-userb-id";
    private const string UserBName = "sssearchuserb";

    private const string VisibleTitleStoryId = "ss-search-story-title-visible";
    private const string VisibleTextStoryId = "ss-search-story-text-visible";
    private const string VisibleChapterStoryId = "ss-search-story-chapter-visible";
    private const string HiddenTitleStoryId = "ss-search-story-title-hidden";
    private const string HiddenChapterStoryId = "ss-search-story-chapter-hidden";

    public StorySearchEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(StorySearchEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            var visibleTitleStory = await DatabaseSeeder.SeedStory(entriesDb, VisibleTitleStoryId, UserAId, isSharingInherited: true);
            visibleTitleStory.Title = "Alpha visible title";

            var visibleTextStory = await DatabaseSeeder.SeedStory(entriesDb, VisibleTextStoryId, UserAId, isSharingInherited: false);
            visibleTextStory.Text = "This story description mentions alpha.";
            await DatabaseSeeder.SeedStoryShare(entriesDb, VisibleTextStoryId, UserBId, Permission.Read);

            var visibleChapterStory = await DatabaseSeeder.SeedStory(entriesDb, VisibleChapterStoryId, UserAId, isSharingInherited: true);
            var visibleChapter = await DatabaseSeeder.SeedChapter(entriesDb, "ss-search-story-chapter-visible-id", visibleChapterStory, title: "Alpha chapter title");
            visibleChapter.Text = "Chapter text";

            var hiddenTitleStory = await DatabaseSeeder.SeedStory(entriesDb, HiddenTitleStoryId, UserAId, isSharingInherited: false);
            hiddenTitleStory.Title = "Alpha hidden title";

            var hiddenChapterStory = await DatabaseSeeder.SeedStory(entriesDb, HiddenChapterStoryId, UserAId, isSharingInherited: false);
            var hiddenChapter = await DatabaseSeeder.SeedChapter(entriesDb, "ss-search-story-chapter-hidden-id", hiddenChapterStory, title: "Alpha hidden chapter");
            hiddenChapter.Text = "Hidden chapter text";

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Search_UserB_ReturnsOnlyAuthorizedStoryMatchesAcrossSupportedFields()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/stories/search?q=Alpha&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<StoryListModel>>();
        var storyIds = page.Models.Select(model => model.Id).ToList();

        Assert.Contains(VisibleTitleStoryId, storyIds);
        Assert.Contains(VisibleTextStoryId, storyIds);
        Assert.Contains(VisibleChapterStoryId, storyIds);
        Assert.DoesNotContain(HiddenTitleStoryId, storyIds);
        Assert.DoesNotContain(HiddenChapterStoryId, storyIds);
        Assert.Equal(3, page.Models.Count);
        Assert.False(page.HasMore);
    }
}
