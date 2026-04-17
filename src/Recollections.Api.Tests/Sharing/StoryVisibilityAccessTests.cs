using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

/// <summary>
/// Covers the anonymous / stranger / public-reader visibility dimensions for
/// story-rooted read endpoints that the existing suite only exercises as an
/// authenticated connected reader: timeline, chapter timeline, map, images, media.
/// </summary>
public class StoryVisibilityAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "svi-owner-id";
    private const string OwnerUserName = "sviowner";
    private const string StrangerUserId = "svi-stranger-id";
    private const string StrangerUserName = "svistranger";

    private const string PrivateStoryId = "svi-story-private";
    private const string PrivateChapterId = "svi-chapter-private";
    private const string PublicStoryId = "svi-story-public";
    private const string PublicChapterId = "svi-chapter-public";

    public StoryVisibilityAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(StoryVisibilityAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            var privateStory = await DatabaseSeeder.SeedStory(entriesDb, PrivateStoryId, OwnerUserId, isSharingInherited: false);
            var privateChapter = await DatabaseSeeder.SeedChapter(entriesDb, PrivateChapterId, privateStory);

            var publicStory = await DatabaseSeeder.SeedStory(entriesDb, PublicStoryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, PublicStoryId, ShareStatusService.PublicUserId, Permission.Read);
            var publicChapter = await DatabaseSeeder.SeedChapter(entriesDb, PublicChapterId, publicStory);

            // An entry in the public story that is itself public — should be visible anonymously.
            var publicEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                "svi-entry-public",
                OwnerUserId,
                isSharingInherited: false,
                story: publicStory,
                when: new DateTime(2024, 9, 1, 10, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryShare(entriesDb, "svi-entry-public", ShareStatusService.PublicUserId, Permission.Read);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, publicEntry, 50.0, 14.0);
            await DatabaseSeeder.SeedImage(entriesDb, "svi-image-public", publicEntry);

            // A public entry in the public chapter
            var publicChapterEntry = await DatabaseSeeder.SeedEntry(
                entriesDb,
                "svi-entry-public-chapter",
                OwnerUserId,
                isSharingInherited: false,
                chapter: publicChapter,
                when: new DateTime(2024, 9, 2, 10, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryShare(entriesDb, "svi-entry-public-chapter", ShareStatusService.PublicUserId, Permission.Read);

            // A private entry in the public story (own visibility, not inheriting) — no shares.
            await DatabaseSeeder.SeedEntry(
                entriesDb,
                "svi-entry-private-inherited",
                OwnerUserId,
                isSharingInherited: false,
                story: publicStory,
                when: new DateTime(2024, 9, 3, 10, 0, 0, DateTimeKind.Utc));
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static IEnumerable<object[]> PrivateStoryRoutes()
    {
        yield return new object[] { $"/api/stories/{PrivateStoryId}/timeline" };
        yield return new object[] { $"/api/stories/{PrivateStoryId}/chapters/{PrivateChapterId}/timeline" };
        yield return new object[] { $"/api/stories/{PrivateStoryId}/map" };
        yield return new object[] { $"/api/stories/{PrivateStoryId}/images" };
        yield return new object[] { $"/api/stories/{PrivateStoryId}/media" };
    }

    public static IEnumerable<object[]> PublicStoryRoutes()
    {
        yield return new object[] { $"/api/stories/{PublicStoryId}/timeline" };
        yield return new object[] { $"/api/stories/{PublicStoryId}/chapters/{PublicChapterId}/timeline" };
        yield return new object[] { $"/api/stories/{PublicStoryId}/map" };
        yield return new object[] { $"/api/stories/{PublicStoryId}/images" };
        yield return new object[] { $"/api/stories/{PublicStoryId}/media" };
    }

    [Theory, MemberData(nameof(PrivateStoryRoutes))]
    public async Task PrivateStory_AsStranger_ReturnsUnauthorized(string route)
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(PrivateStoryRoutes))]
    public async Task PrivateStory_AsAnonymous_ReturnsUnauthorized(string route)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory, MemberData(nameof(PublicStoryRoutes))]
    public async Task PublicStory_AsAnonymous_ReturnsOk(string route)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PublicStory_Timeline_AsAnonymous_FiltersOutInheritedEntries()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/stories/{PublicStoryId}/timeline");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.ReadJsonAsync<PageableList<EntryListModel>>();
        var entryIds = page.Models.Select(e => e.Id).ToList();

        Assert.Contains("svi-entry-public", entryIds);
        Assert.DoesNotContain("svi-entry-private-inherited", entryIds);
        Assert.DoesNotContain("svi-entry-public-chapter", entryIds); // chapter entries aren't in story timeline
    }

    [Fact]
    public async Task PublicStory_Images_AsAnonymous_ReturnsOnlyPublicImages()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/stories/{PublicStoryId}/images");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryImagesModel>>();
        var entryIds = models.Select(m => m.EntryId).ToList();

        Assert.Contains("svi-entry-public", entryIds);
        Assert.DoesNotContain("svi-entry-private-inherited", entryIds);
    }
}
