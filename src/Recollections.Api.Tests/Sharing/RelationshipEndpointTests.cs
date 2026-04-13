using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class ProfileTimelineAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "rt-profile-usera-id";
    private const string UserAName = "rtprofileusera";
    private const string UserBId = "rt-profile-userb-id";
    private const string UserBName = "rtprofileuserb";

    private const string InheritedVisibleEntryId = "rt-profile-entry-visible-inherited";
    private const string ExplicitVisibleEntryId = "rt-profile-entry-visible-explicit";
    private const string PrivateHiddenEntryId = "rt-profile-entry-hidden-private";

    public ProfileTimelineAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(ProfileTimelineAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            await DatabaseSeeder.SeedUserBeing(entriesDb, UserAId, UserAName);
            await DatabaseSeeder.SeedBeingShare(entriesDb, UserAId, UserBId, Permission.Read);

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                InheritedVisibleEntryId,
                UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                ExplicitVisibleEntryId,
                UserAId,
                isSharingInherited: false,
                when: new DateTime(2024, 6, 2, 10, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryShare(entriesDb, ExplicitVisibleEntryId, UserBId, Permission.Read);

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                PrivateHiddenEntryId,
                UserAId,
                isSharingInherited: false,
                when: new DateTime(2024, 6, 3, 10, 0, 0, DateTimeKind.Utc));
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ProfileTimeline_UserB_ReturnsOnlyAccessibleEntriesOfTheProfileOwner()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/profiles/{UserAId}/timeline/list?offset=0&count=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains(InheritedVisibleEntryId, entryIds);
        Assert.Contains(ExplicitVisibleEntryId, entryIds);
        Assert.DoesNotContain(PrivateHiddenEntryId, entryIds);
        Assert.Equal(2, page.Models.Count);
        Assert.All(page.Models, model => Assert.Equal(UserAId, model.UserId));
        Assert.False(page.HasMore);
    }
}

public class StoryRelationshipTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "rt-story-usera-id";
    private const string UserAName = "rtstoryusera";
    private const string UserBId = "rt-story-userb-id";
    private const string UserBName = "rtstoryuserb";

    private const string StoryId = "rt-story-id";
    private const string ChapterId = "rt-story-chapter-id";
    private const string StoryVisibleDirectEntryId = "rt-story-entry-direct-visible";
    private const string StoryHiddenDirectEntryId = "rt-story-entry-direct-hidden";
    private const string ChapterVisibleEntryId = "rt-story-entry-chapter-visible";
    private const string ChapterHiddenEntryId = "rt-story-entry-chapter-hidden";

    public StoryRelationshipTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(StoryRelationshipTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);

            var story = await DatabaseSeeder.SeedStory(entriesDb, StoryId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, StoryId, UserBId, Permission.Read);

            var chapter = await DatabaseSeeder.SeedChapter(entriesDb, ChapterId, story);

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                StoryVisibleDirectEntryId,
                UserAId,
                isSharingInherited: true,
                story: story,
                when: new DateTime(2024, 7, 1, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                StoryHiddenDirectEntryId,
                UserAId,
                isSharingInherited: false,
                story: story,
                when: new DateTime(2024, 7, 2, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                ChapterVisibleEntryId,
                UserAId,
                isSharingInherited: true,
                chapter: chapter,
                when: new DateTime(2024, 7, 3, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                ChapterHiddenEntryId,
                UserAId,
                isSharingInherited: false,
                chapter: chapter,
                when: new DateTime(2024, 7, 4, 10, 0, 0, DateTimeKind.Utc));
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task StoryTimeline_UserB_ReturnsOnlyVisibleDirectStoryEntries()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/stories/{StoryId}/timeline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains(StoryVisibleDirectEntryId, entryIds);
        Assert.DoesNotContain(StoryHiddenDirectEntryId, entryIds);
        Assert.DoesNotContain(ChapterVisibleEntryId, entryIds);
        Assert.DoesNotContain(ChapterHiddenEntryId, entryIds);
        Assert.Single(page.Models);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task ChapterTimeline_UserB_ReturnsOnlyVisibleEntriesInThatChapter()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/stories/{StoryId}/chapters/{ChapterId}/timeline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains(ChapterVisibleEntryId, entryIds);
        Assert.DoesNotContain(ChapterHiddenEntryId, entryIds);
        Assert.DoesNotContain(StoryVisibleDirectEntryId, entryIds);
        Assert.DoesNotContain(StoryHiddenDirectEntryId, entryIds);
        Assert.Single(page.Models);
        Assert.False(page.HasMore);
    }
}

public class BeingRelationshipTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "rt-being-usera-id";
    private const string UserAName = "rtbeingusera";
    private const string UserBId = "rt-being-userb-id";
    private const string UserBName = "rtbeinguserb";

    private const string BeingId = "rt-being-id";
    private const string VisibleStoryId = "rt-being-story-visible";
    private const string VisibleStoryChapterId = "rt-being-story-visible-chapter";
    private const string HiddenStoryId = "rt-being-story-hidden";
    private const string VisibleEntryId = "rt-being-entry-visible";
    private const string VisibleChapterEntryId = "rt-being-entry-visible-chapter";
    private const string HiddenEntryInVisibleStoryId = "rt-being-entry-hidden-visible-story";
    private const string HiddenEntryInHiddenStoryId = "rt-being-entry-hidden-hidden-story";

    public BeingRelationshipTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(BeingRelationshipTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);

            var being = await DatabaseSeeder.SeedBeing(entriesDb, BeingId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, BeingId, UserBId, Permission.Read);

            var visibleStory = await DatabaseSeeder.SeedStory(entriesDb, VisibleStoryId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, VisibleStoryId, UserBId, Permission.Read);
            var visibleStoryChapter = await DatabaseSeeder.SeedChapter(entriesDb, VisibleStoryChapterId, visibleStory);

            var hiddenStory = await DatabaseSeeder.SeedStory(entriesDb, HiddenStoryId, UserAId, isSharingInherited: false);

            var visibleEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleEntryId,
                UserAId,
                isSharingInherited: true,
                story: visibleStory,
                when: new DateTime(2024, 8, 1, 10, 0, 0, DateTimeKind.Utc));

            var visibleChapterEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleChapterEntryId,
                UserAId,
                isSharingInherited: true,
                chapter: visibleStoryChapter,
                when: new DateTime(2024, 8, 1, 11, 0, 0, DateTimeKind.Utc));

            var hiddenEntryInVisibleStory = await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenEntryInVisibleStoryId,
                UserAId,
                isSharingInherited: false,
                story: visibleStory,
                when: new DateTime(2024, 8, 2, 10, 0, 0, DateTimeKind.Utc));

            var hiddenEntryInHiddenStory = await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenEntryInHiddenStoryId,
                UserAId,
                isSharingInherited: true,
                story: hiddenStory,
                when: new DateTime(2024, 8, 3, 10, 0, 0, DateTimeKind.Utc));

            visibleEntry.Beings.Add(being);
            visibleChapterEntry.Beings.Add(being);
            hiddenEntryInVisibleStory.Beings.Add(being);
            hiddenEntryInHiddenStory.Beings.Add(being);

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BeingTimeline_UserB_UsesTheSharedBeingPathForInheritedEntries()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/beings/{BeingId}/timeline?offset=0&count=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains(VisibleEntryId, entryIds);
        Assert.Contains(VisibleChapterEntryId, entryIds);
        Assert.Contains(HiddenEntryInHiddenStoryId, entryIds);
        Assert.DoesNotContain(HiddenEntryInVisibleStoryId, entryIds);
        Assert.Equal(3, page.Models.Count);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task BeingStories_UserB_DeduplicatesAccessibleEntriesThatBelongToTheSameStory()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/beings/{BeingId}/stories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<StoryListModel>>();
        var storyIds = models.Select(m => m.Id).ToList();
        var modelById = models.ToDictionary(m => m.Id);

        Assert.Contains(VisibleStoryId, storyIds);
        Assert.Contains(HiddenStoryId, storyIds);
        Assert.Equal(2, models.Count);
        Assert.Equal(2, modelById[VisibleStoryId].Entries);
        Assert.Equal(1, modelById[HiddenStoryId].Entries);
    }
}
