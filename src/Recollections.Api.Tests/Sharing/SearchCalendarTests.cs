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

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Search_UserB_ReturnsOnlyAuthorizedMatchesAcrossSupportedFields()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var response = await client.GetAsync("/api/search?q=Alpha&offset=0");

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
