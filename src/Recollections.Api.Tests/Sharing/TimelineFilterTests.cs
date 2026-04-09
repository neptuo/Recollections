using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class TimelineFilterTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "tl-usera-id";
    private const string UserAName = "tlusera";
    private const string UserBId = "tl-userb-id";
    private const string UserBName = "tluserb";
    private const string UserCId = "tl-userc-id";
    private const string UserCName = "tluserc";

    // UserA's entries
    private const string EntryOwnedByA = "tl-entry-a";
    private const string EntrySharedWithB = "tl-entry-shared-b";
    private const string EntryPrivateA = "tl-entry-private-a";

    // UserB's entry
    private const string EntryOwnedByB = "tl-entry-b";
    private const string EntryOwnedByC = "tl-entry-c";

    public TimelineFilterTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(TimelineFilterTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedUser(accountsDb, UserCId, UserCName);

            // A grants Read to B via connection
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId,
                Permission.Read, Permission.Read);

            // No connection between A and C, or B and C

            // A's inherited entry (visible to B via connection)
            await DatabaseSeeder.SeedEntry(entriesDb, EntryOwnedByA, UserAId, isSharingInherited: true);

            // A's explicitly shared entry with B
            await DatabaseSeeder.SeedEntry(entriesDb, EntrySharedWithB, UserAId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, EntrySharedWithB, UserBId, Permission.Read);

            // A's private entry (not shared, not inherited since no story)
            await DatabaseSeeder.SeedEntry(entriesDb, EntryPrivateA, UserAId, isSharingInherited: false);

            // B's own entry
            await DatabaseSeeder.SeedEntry(entriesDb, EntryOwnedByB, UserBId, isSharingInherited: true);

            // C's own entry
            await DatabaseSeeder.SeedEntry(entriesDb, EntryOwnedByC, UserCId, isSharingInherited: true);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<PageableList<EntryListModel>> GetTimelineAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/timeline/list");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.ReadJsonAsync<PageableList<EntryListModel>>();
    }

    [Fact]
    public async Task Timeline_UserA_SeesOwnAndConnectedEntries()
    {
        var client = factory.CreateClientForUser(UserAId, UserAName);
        var page = await GetTimelineAsync(client);
        var entryIds = page.Models.Select(e => e.Id).ToList();

        // A sees own entries
        Assert.Contains(EntryOwnedByA, entryIds);
        Assert.Contains(EntrySharedWithB, entryIds);
        Assert.Contains(EntryPrivateA, entryIds);

        // A also sees B's inherited entry (bidirectional connection, B grants Read to A)
        Assert.Contains(EntryOwnedByB, entryIds);

        // A does not see C's private entry
        Assert.DoesNotContain(EntryOwnedByC, entryIds);
        Assert.Equal(4, entryIds.Count);
        Assert.True(page.HasMore is false);
    }

    [Fact]
    public async Task Timeline_UserB_SeesOwnAndSharedEntries()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var page = await GetTimelineAsync(client);
        var entryIds = page.Models.Select(e => e.Id).ToList();

        // B sees own entries
        Assert.Contains(EntryOwnedByB, entryIds);

        // B sees A's inherited entry (via connection)
        Assert.Contains(EntryOwnedByA, entryIds);

        // B sees A's explicitly shared entry
        Assert.Contains(EntrySharedWithB, entryIds);

        // B does NOT see A's private entry
        Assert.DoesNotContain(EntryPrivateA, entryIds);

        // B does NOT see C's private entry
        Assert.DoesNotContain(EntryOwnedByC, entryIds);
        Assert.Equal(3, entryIds.Count);
        Assert.True(page.HasMore is false);
    }

    [Fact]
    public async Task Timeline_UserC_SeesOnlyOwnEntries()
    {
        var client = factory.CreateClientForUser(UserCId, UserCName);
        var page = await GetTimelineAsync(client);
        var entryIds = page.Models.Select(e => e.Id).ToList();

        // C sees own entry
        Assert.Contains(EntryOwnedByC, entryIds);

        // C sees nothing from A or B
        Assert.DoesNotContain(EntryOwnedByA, entryIds);
        Assert.DoesNotContain(EntrySharedWithB, entryIds);
        Assert.DoesNotContain(EntryPrivateA, entryIds);
        Assert.DoesNotContain(EntryOwnedByB, entryIds);
        Assert.Single(entryIds);
        Assert.True(page.HasMore is false);
    }

    [Fact]
    public async Task Timeline_Anonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/timeline/list");
        // Timeline requires [Authorize]
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
