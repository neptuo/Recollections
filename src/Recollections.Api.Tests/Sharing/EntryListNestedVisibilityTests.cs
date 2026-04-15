using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class EntryListNestedVisibilityTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string JonUserId = "elnv-jon-id";
    private const string JonUserName = "elnvjon";
    private const string JaneUserId = "elnv-jane-id";
    private const string JaneUserName = "elnvjane";

    private const string AliceBeingId = "elnv-being-alice";
    private const string PeterBeingId = "elnv-being-peter";

    private const string SharedStoryId = "elnv-story-shared";
    private const string SharedStoryTitle = "Shared nested story";
    private const string SharedChapterId = "elnv-chapter-shared";
    private const string SharedChapterTitle = "Shared nested chapter";

    private const string HiddenStoryId = "elnv-story-hidden";
    private const string HiddenStoryTitle = "Hidden metadata story";
    private const string HiddenChapterId = "elnv-chapter-hidden";
    private const string HiddenChapterTitle = "Hidden metadata chapter";

    private const string DirectVisibleEntryId = "elnv-entry-direct-visible";
    private const string DirectHiddenEntryId = "elnv-entry-direct-hidden";
    private const string ChapterVisibleEntryId = "elnv-entry-chapter-visible";
    private const string HiddenStorySharedEntryId = "elnv-entry-hidden-story-shared";
    private const string SearchTerm = "NestedBeings";

    private static readonly DateTime OnThisDayDate = CreatePastOnThisDayDate();
    private static readonly DateTime HiddenStoryEntryDate = CreateHiddenStoryEntryDate();

    public EntryListNestedVisibilityTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryListNestedVisibilityTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, JonUserId, JonUserName);
            await DatabaseSeeder.SeedUser(accountsDb, JaneUserId, JaneUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, JonUserId, JaneUserId, permission1: null, permission2: null);

            await DatabaseSeeder.SeedUserBeing(entriesDb, JonUserId, JonUserName);
            await DatabaseSeeder.SeedBeingShare(entriesDb, JonUserId, JaneUserId, Permission.Read);

            var alice = await DatabaseSeeder.SeedBeing(entriesDb, AliceBeingId, JonUserId, isSharingInherited: false);
            var peter = await DatabaseSeeder.SeedBeing(entriesDb, PeterBeingId, JonUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, AliceBeingId, JaneUserId, Permission.Read);

            var sharedStory = await DatabaseSeeder.SeedStory(entriesDb, SharedStoryId, JonUserId, isSharingInherited: false);
            sharedStory.Title = SharedStoryTitle;
            await DatabaseSeeder.SeedStoryShare(entriesDb, SharedStoryId, JaneUserId, Permission.Read);
            var sharedChapter = await DatabaseSeeder.SeedChapter(entriesDb, SharedChapterId, sharedStory, title: SharedChapterTitle);

            var hiddenStory = await DatabaseSeeder.SeedStory(entriesDb, HiddenStoryId, JonUserId, isSharingInherited: false);
            hiddenStory.Title = HiddenStoryTitle;
            var hiddenChapter = await DatabaseSeeder.SeedChapter(entriesDb, HiddenChapterId, hiddenStory, title: HiddenChapterTitle);

            var directVisibleEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                DirectVisibleEntryId,
                JonUserId,
                isSharingInherited: true,
                story: sharedStory,
                title: $"{SearchTerm} direct visible",
                when: OnThisDayDate);

            var directHiddenEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                DirectHiddenEntryId,
                JonUserId,
                isSharingInherited: false,
                story: sharedStory,
                title: $"{SearchTerm} direct hidden",
                when: OnThisDayDate);

            var chapterVisibleEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                ChapterVisibleEntryId,
                JonUserId,
                isSharingInherited: true,
                chapter: sharedChapter,
                title: $"{SearchTerm} chapter visible",
                when: OnThisDayDate.AddMinutes(1));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenStorySharedEntryId,
                JonUserId,
                isSharingInherited: false,
                chapter: hiddenChapter,
                title: "Hidden story metadata entry",
                when: HiddenStoryEntryDate);
            await DatabaseSeeder.SeedEntryShare(entriesDb, HiddenStorySharedEntryId, JaneUserId, Permission.Read);

            directVisibleEntry.Beings.Add(alice);
            directVisibleEntry.Beings.Add(peter);
            directHiddenEntry.Beings.Add(alice);
            directHiddenEntry.Beings.Add(peter);
            chapterVisibleEntry.Beings.Add(alice);
            chapterVisibleEntry.Beings.Add(peter);

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static DateTime CreatePastOnThisDayDate()
    {
        var today = DateTime.Today;
        var candidate = today.AddYears(-1);
        if (candidate.Month != today.Month || candidate.Day != today.Day)
            candidate = today.AddYears(-4);

        return candidate.AddHours(10);
    }

    private static DateTime CreateHiddenStoryEntryDate()
    {
        int month = OnThisDayDate.Month == 1 ? 2 : 1;
        return new DateTime(OnThisDayDate.Year, month, 15, 10, 0, 0);
    }

    private async Task<PageableList<EntryListModel>> GetPageAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<PageableList<EntryListModel>>();
    }

    private async Task<List<EntryListModel>> GetListAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<List<EntryListModel>>();
    }

    private async Task<EntryStoryModel> GetEntryStoryAsync(HttpClient client, string entryId)
    {
        var response = await client.GetAsync($"/api/entries/{entryId}/story");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<EntryStoryModel>();
    }

    private static void AssertOnlyAlice(EntryListModel model)
    {
        Assert.Collection(
            model.Beings,
            being => Assert.Equal(AliceBeingId, being.Id));
    }

    [Fact]
    public async Task Timeline_User_OnlyReturnsVisibleAttachedBeingsInEntryModels()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var page = await GetPageAsync(client, "/api/timeline/list?offset=0&count=20");
        var modelById = page.Models.ToDictionary(model => model.Id);

        Assert.Contains(DirectVisibleEntryId, modelById.Keys);
        Assert.Contains(ChapterVisibleEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectHiddenEntryId, modelById.Keys);
        AssertOnlyAlice(modelById[DirectVisibleEntryId]);
        AssertOnlyAlice(modelById[ChapterVisibleEntryId]);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task ProfileTimeline_User_FiltersAttachedBeingsAndKeepsAssociatedStoryMetadata()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var page = await GetPageAsync(client, $"/api/profiles/{JonUserId}/timeline/list?offset=0&count=20");
        var modelById = page.Models.ToDictionary(model => model.Id);

        Assert.Contains(DirectVisibleEntryId, modelById.Keys);
        Assert.Contains(ChapterVisibleEntryId, modelById.Keys);
        Assert.Contains(HiddenStorySharedEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectHiddenEntryId, modelById.Keys);
        AssertOnlyAlice(modelById[DirectVisibleEntryId]);
        AssertOnlyAlice(modelById[ChapterVisibleEntryId]);
        Assert.Equal(HiddenStoryTitle, modelById[HiddenStorySharedEntryId].StoryTitle);
        Assert.Equal(HiddenChapterTitle, modelById[HiddenStorySharedEntryId].ChapterTitle);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task StoryTimeline_User_OnlyReturnsVisibleAttachedBeingsForDirectStoryEntries()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var page = await GetPageAsync(client, $"/api/stories/{SharedStoryId}/timeline");
        var modelById = page.Models.ToDictionary(model => model.Id);

        Assert.Contains(DirectVisibleEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectHiddenEntryId, modelById.Keys);
        Assert.DoesNotContain(ChapterVisibleEntryId, modelById.Keys);
        AssertOnlyAlice(modelById[DirectVisibleEntryId]);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task ChapterTimeline_User_OnlyReturnsVisibleAttachedBeingsForChapterEntries()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var page = await GetPageAsync(client, $"/api/stories/{SharedStoryId}/chapters/{SharedChapterId}/timeline");
        var modelById = page.Models.ToDictionary(model => model.Id);

        Assert.Contains(ChapterVisibleEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectVisibleEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectHiddenEntryId, modelById.Keys);
        AssertOnlyAlice(modelById[ChapterVisibleEntryId]);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task BeingTimeline_User_OnlyReturnsVisibleAttachedBeingsForEntriesTaggedWithThatBeing()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var page = await GetPageAsync(client, $"/api/beings/{AliceBeingId}/timeline?offset=0&count=20");
        var modelById = page.Models.ToDictionary(model => model.Id);

        Assert.Contains(DirectVisibleEntryId, modelById.Keys);
        Assert.Contains(ChapterVisibleEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectHiddenEntryId, modelById.Keys);
        AssertOnlyAlice(modelById[DirectVisibleEntryId]);
        AssertOnlyAlice(modelById[ChapterVisibleEntryId]);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task Search_User_OnlyReturnsVisibleAttachedBeingsInSearchResults()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var page = await GetPageAsync(client, $"/api/search?q={SearchTerm}&offset=0");
        var modelById = page.Models.ToDictionary(model => model.Id);

        Assert.Contains(DirectVisibleEntryId, modelById.Keys);
        Assert.Contains(ChapterVisibleEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectHiddenEntryId, modelById.Keys);
        AssertOnlyAlice(modelById[DirectVisibleEntryId]);
        AssertOnlyAlice(modelById[ChapterVisibleEntryId]);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task CalendarMonth_User_OnlyReturnsVisibleAttachedBeingsInCalendarResults()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var models = await GetListAsync(client, $"/api/calendar/{OnThisDayDate.Year}/{OnThisDayDate.Month}");
        var modelById = models.ToDictionary(model => model.Id);

        Assert.Contains(DirectVisibleEntryId, modelById.Keys);
        Assert.Contains(ChapterVisibleEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectHiddenEntryId, modelById.Keys);
        AssertOnlyAlice(modelById[DirectVisibleEntryId]);
        AssertOnlyAlice(modelById[ChapterVisibleEntryId]);
    }

    [Fact]
    public async Task OnThisDayList_User_OnlyReturnsVisibleAttachedBeings()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var models = await GetListAsync(client, "/api/on-this-day");
        var modelById = models.ToDictionary(model => model.Id);

        Assert.Contains(DirectVisibleEntryId, modelById.Keys);
        Assert.Contains(ChapterVisibleEntryId, modelById.Keys);
        Assert.DoesNotContain(DirectHiddenEntryId, modelById.Keys);
        Assert.Equal(2, models.Count);
        AssertOnlyAlice(modelById[DirectVisibleEntryId]);
        AssertOnlyAlice(modelById[ChapterVisibleEntryId]);
    }

    [Fact]
    public async Task OnThisDayCount_User_CountsOnlyVisibleEntries()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var response = await client.GetAsync("/api/on-this-day/count");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var count = await response.ReadJsonAsync<int>();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task EntryStory_User_ReturnsAssociatedStoryMetadataForVisibleEntryInUnsharedStory()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var model = await GetEntryStoryAsync(client, HiddenStorySharedEntryId);

        Assert.Equal(HiddenStoryId, model.StoryId);
        Assert.Equal(HiddenStoryTitle, model.StoryTitle);
        Assert.Equal(HiddenChapterId, model.ChapterId);
        Assert.Equal(HiddenChapterTitle, model.ChapterTitle);
    }
}
