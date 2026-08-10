using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class SearchEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "sc-search-usera-id";
    private const string UserAName = "scsearchusera";
    private const string UserBId = "sc-search-userb-id";
    private const string UserBName = "scsearchuserb";

    private const string SharedStoryId = "sc-search-story-id";
    private const string SharedChapterId = "sc-search-chapter-id";
    private const string TitleVisibleEntryId = "sc-search-entry-title-visible";
    private const string TextVisibleEntryId = "sc-search-entry-text-visible";
    private const string StoryVisibleEntryId = "sc-search-entry-story-visible";
    private const string ChapterVisibleEntryId = "sc-search-entry-chapter-visible";
    private const string HiddenTitleEntryId = "sc-search-entry-title-hidden";
    private const string HiddenChapterEntryId = "sc-search-entry-chapter-hidden";
    private const string FirstBeingId = "sc-search-being-first";
    private const string SecondBeingId = "sc-search-being-second";
    private const string ThirdBeingId = "sc-search-being-third";

    public SearchEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(SearchEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            var sharedStory = await DatabaseSeeder.SeedStory(entriesDb, SharedStoryId, UserAId, isSharingInherited: false);
            sharedStory.Title = "Alpha shared story";

            var sharedChapter = await DatabaseSeeder.SeedChapter(entriesDb, SharedChapterId, sharedStory, title: "Alpha shared chapter");
            await DatabaseSeeder.SeedStoryShare(entriesDb, SharedStoryId, UserBId, Permission.Read);

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                TitleVisibleEntryId,
                UserAId,
                isSharingInherited: true,
                title: "Alpha title match",
                when: new DateTime(2024, 9, 1, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                TextVisibleEntryId,
                UserAId,
                isSharingInherited: false,
                title: "Explicit text match",
                text: "This entry contains alpha in the text.",
                when: new DateTime(2024, 9, 2, 10, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryShare(entriesDb, TextVisibleEntryId, UserBId, Permission.Read);

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                StoryVisibleEntryId,
                UserAId,
                isSharingInherited: true,
                story: sharedStory,
                title: "Story title match",
                when: new DateTime(2024, 9, 3, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                ChapterVisibleEntryId,
                UserAId,
                isSharingInherited: true,
                chapter: sharedChapter,
                title: "Chapter title match",
                when: new DateTime(2024, 9, 4, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenTitleEntryId,
                UserAId,
                isSharingInherited: false,
                title: "Alpha hidden title",
                when: new DateTime(2024, 9, 5, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenChapterEntryId,
                UserAId,
                isSharingInherited: false,
                chapter: sharedChapter,
                title: "Hidden chapter match",
                when: new DateTime(2024, 9, 6, 10, 0, 0, DateTimeKind.Utc));

            var firstBeing = await DatabaseSeeder.SeedBeing(entriesDb, FirstBeingId, UserAId, isSharingInherited: false);
            var secondBeing = await DatabaseSeeder.SeedBeing(entriesDb, SecondBeingId, UserAId, isSharingInherited: false);
            var thirdBeing = await DatabaseSeeder.SeedBeing(entriesDb, ThirdBeingId, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, FirstBeingId, UserBId, Permission.Read);
            await DatabaseSeeder.SeedBeingShare(entriesDb, SecondBeingId, UserBId, Permission.Read);
            await DatabaseSeeder.SeedBeingShare(entriesDb, ThirdBeingId, UserBId, Permission.Read);

            entriesDb.Entries.Single(e => e.Id == TitleVisibleEntryId).Beings.Add(firstBeing);
            entriesDb.Entries.Single(e => e.Id == TextVisibleEntryId).Beings.Add(secondBeing);
            entriesDb.Entries.Single(e => e.Id == StoryVisibleEntryId).Beings.Add(firstBeing);
            entriesDb.Entries.Single(e => e.Id == StoryVisibleEntryId).Beings.Add(secondBeing);
            entriesDb.Entries.Single(e => e.Id == ChapterVisibleEntryId).Beings.Add(thirdBeing);

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Search_UserB_ReturnsOnlyAuthorizedMatchesAcrossSupportedFields()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/entries/search?q=Alpha&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains(TitleVisibleEntryId, entryIds);
        Assert.Contains(TextVisibleEntryId, entryIds);
        Assert.Contains(StoryVisibleEntryId, entryIds);
        Assert.Contains(ChapterVisibleEntryId, entryIds);
        Assert.DoesNotContain(HiddenTitleEntryId, entryIds);
        Assert.DoesNotContain(HiddenChapterEntryId, entryIds);
        Assert.Equal(4, page.Models.Count);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task Search_LegacyEndpoint_UsesEntrySearchBehavior()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/search?q=Alpha&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        Assert.Equal(4, page.Models.Count);
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task Search_UserB_ByAllSelectedBeings_ReturnsOnlyEntriesWithTheCompleteCombination()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/entries/search?being={FirstBeingId}&being={SecondBeingId}&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains(StoryVisibleEntryId, entryIds);
        Assert.DoesNotContain(TitleVisibleEntryId, entryIds);
        Assert.DoesNotContain(TextVisibleEntryId, entryIds);
        Assert.DoesNotContain(ChapterVisibleEntryId, entryIds);
        Assert.Equal(1, page.Models.Count);
    }

    [Fact]
    public async Task Search_UserB_ByDateRangeWithoutPhrase_ReturnsAccessibleEntriesWithinInclusiveBounds()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/entries/search?from=2024-09-02&to=2024-09-03&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains(TextVisibleEntryId, entryIds);
        Assert.Contains(StoryVisibleEntryId, entryIds);
        Assert.DoesNotContain(TitleVisibleEntryId, entryIds);
        Assert.DoesNotContain(ChapterVisibleEntryId, entryIds);
        Assert.DoesNotContain(HiddenTitleEntryId, entryIds);
        Assert.Equal(2, page.Models.Count);
    }

    [Fact]
    public async Task Search_UserB_BySingleDateBoundWithoutPhrase_ReturnsAccessibleEntriesOnTheIncludedSide()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);

        var fromResponse = await client.GetAsync("/api/entries/search?from=2024-09-03&offset=0");
        Assert.Equal(HttpStatusCode.OK, fromResponse.StatusCode);

        var fromPage = await fromResponse.ReadJsonAsync<PageableList<EntryListModel>>();
        Assert.Contains(StoryVisibleEntryId, fromPage.Models.Select(e => e.Id));
        Assert.Contains(ChapterVisibleEntryId, fromPage.Models.Select(e => e.Id));
        Assert.Equal(2, fromPage.Models.Count);

        var toResponse = await client.GetAsync("/api/entries/search?to=2024-09-02&offset=0");
        Assert.Equal(HttpStatusCode.OK, toResponse.StatusCode);

        var toPage = await toResponse.ReadJsonAsync<PageableList<EntryListModel>>();
        Assert.Contains(TitleVisibleEntryId, toPage.Models.Select(e => e.Id));
        Assert.Contains(TextVisibleEntryId, toPage.Models.Select(e => e.Id));
        Assert.Equal(2, toPage.Models.Count);
    }

    [Fact]
    public async Task Search_UserB_ByPhraseBeingAndDate_ReturnsEntriesMatchingEveryFilter()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync($"/api/entries/search?q=Alpha&being={SecondBeingId}&from=2024-09-03&to=2024-09-03&offset=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        Assert.Collection(page.Models, entry => Assert.Equal(StoryVisibleEntryId, entry.Id));
    }
}

public class CalendarEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "sc-calendar-usera-id";
    private const string UserAName = "sccalendarusera";
    private const string UserBId = "sc-calendar-userb-id";
    private const string UserBName = "sccalendaruserb";
    private const string UserDId = "sc-calendar-userd-id";
    private const string UserDName = "sccalendaruserd";

    private const string VisibleInheritedFebruaryEntryId = "sc-calendar-entry-feb-visible";
    private const string VisibleExplicitNovemberEntryId = "sc-calendar-entry-nov-visible";
    private const string HiddenPrivateAugustEntryId = "sc-calendar-entry-aug-hidden";
    private const string VisibleInheritedMarchEntryId = "sc-calendar-entry-mar-visible";
    private const string VisibleInheritedOtherYearEntryId = "sc-calendar-entry-other-year-visible";
    private const string OwnedByBFebruaryEntryId = "sc-calendar-entry-b-feb-visible";

    public CalendarEndpointTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(CalendarEndpointTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedUser(accountsDb, UserDId, UserDName, isPremium: false);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleInheritedFebruaryEntryId,
                UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 2, 10, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleExplicitNovemberEntryId,
                UserAId,
                isSharingInherited: false,
                when: new DateTime(2024, 11, 11, 10, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryShare(entriesDb, VisibleExplicitNovemberEntryId, UserBId, Permission.Read);

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenPrivateAugustEntryId,
                UserAId,
                isSharingInherited: false,
                when: new DateTime(2024, 8, 8, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleInheritedMarchEntryId,
                UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 3, 4, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                VisibleInheritedOtherYearEntryId,
                UserAId,
                isSharingInherited: true,
                when: new DateTime(2023, 2, 10, 10, 0, 0, DateTimeKind.Utc));

            await DatabaseSeeder.SeedEntry(
                entriesDb,
                OwnedByBFebruaryEntryId,
                UserBId,
                isSharingInherited: true,
                when: new DateTime(2024, 2, 20, 10, 0, 0, DateTimeKind.Utc));
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CalendarYear_UserB_ReturnsOnlyAccessibleEntriesForThatYear()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/calendar/2024");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryListModel>>();
        var entryIds = models.Select(model => model.Id).ToList();

        Assert.Contains(VisibleInheritedFebruaryEntryId, entryIds);
        Assert.Contains(VisibleExplicitNovemberEntryId, entryIds);
        Assert.Contains(VisibleInheritedMarchEntryId, entryIds);
        Assert.Contains(OwnedByBFebruaryEntryId, entryIds);
        Assert.DoesNotContain(HiddenPrivateAugustEntryId, entryIds);
        Assert.DoesNotContain(VisibleInheritedOtherYearEntryId, entryIds);
        Assert.Equal(4, models.Count);
    }

    [Fact]
    public async Task CalendarMonth_UserB_ReturnsOnlyAccessibleEntriesForThatMonth()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/calendar/2024/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryListModel>>();
        var entryIds = models.Select(model => model.Id).ToList();

        Assert.Contains(VisibleInheritedFebruaryEntryId, entryIds);
        Assert.Contains(OwnedByBFebruaryEntryId, entryIds);
        Assert.DoesNotContain(VisibleExplicitNovemberEntryId, entryIds);
        Assert.DoesNotContain(HiddenPrivateAugustEntryId, entryIds);
        Assert.DoesNotContain(VisibleInheritedMarchEntryId, entryIds);
        Assert.DoesNotContain(VisibleInheritedOtherYearEntryId, entryIds);
        Assert.Equal(2, models.Count);
    }

    [Fact]
    public async Task CalendarYear_NonPremiumUser_ReturnsPaymentRequired()
    {
        var client = factory.CreateClientForUser(UserDId, UserDName);
        var response = await client.GetAsync("/api/calendar/2024");

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
    }
}
