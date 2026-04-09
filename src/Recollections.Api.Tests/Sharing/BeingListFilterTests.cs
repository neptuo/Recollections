using System.Net;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class BeingListFilterTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "bl-usera-id";
    private const string UserAName = "blusera";
    private const string UserBId = "bl-userb-id";
    private const string UserBName = "bluserb";
    private const string UserCId = "bl-userc-id";
    private const string UserCName = "bluserc";

    private const string BeingOwnedByAInherited = "bl-being-a-inherited";
    private const string BeingSharedWithB = "bl-being-a-shared";
    private const string BeingPrivateA = "bl-being-a-private";
    private const string BeingOwnedByBInherited = "bl-being-b-inherited";
    private const string BeingOwnedByCPrivate = "bl-being-c-private";

    public BeingListFilterTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(BeingListFilterTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedUser(accountsDb, UserCId, UserCName);

            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            var beingOwnedByAInherited = await DatabaseSeeder.SeedBeing(entriesDb, BeingOwnedByAInherited, UserAId, isSharingInherited: true);
            var beingSharedWithB = await DatabaseSeeder.SeedBeing(entriesDb, BeingSharedWithB, UserAId, isSharingInherited: false);
            var beingPrivateA = await DatabaseSeeder.SeedBeing(entriesDb, BeingPrivateA, UserAId, isSharingInherited: false);
            var beingOwnedByBInherited = await DatabaseSeeder.SeedBeing(entriesDb, BeingOwnedByBInherited, UserBId, isSharingInherited: true);
            var beingOwnedByCPrivate = await DatabaseSeeder.SeedBeing(entriesDb, BeingOwnedByCPrivate, UserCId, isSharingInherited: false);

            await DatabaseSeeder.SeedBeingShare(entriesDb, BeingSharedWithB, UserBId, Permission.Read);

            var entryOwnedByAInherited = await DatabaseSeeder.SeedEntry(entriesDb, "bl-entry-a-inherited", UserAId, isSharingInherited: true);
            entryOwnedByAInherited.Beings.Add(beingOwnedByAInherited);

            var entrySharedWithBHidden = await DatabaseSeeder.SeedEntry(entriesDb, "bl-entry-a-shared-hidden", UserAId, isSharingInherited: false);
            entrySharedWithBHidden.Beings.Add(beingSharedWithB);

            var entryPrivateA = await DatabaseSeeder.SeedEntry(entriesDb, "bl-entry-a-private", UserAId, isSharingInherited: false);
            entryPrivateA.Beings.Add(beingPrivateA);

            var entryOwnedByBInherited = await DatabaseSeeder.SeedEntry(entriesDb, "bl-entry-b-inherited", UserBId, isSharingInherited: true);
            entryOwnedByBInherited.Beings.Add(beingOwnedByBInherited);

            var entryOwnedByCPrivate = await DatabaseSeeder.SeedEntry(entriesDb, "bl-entry-c-private", UserCId, isSharingInherited: false);
            entryOwnedByCPrivate.Beings.Add(beingOwnedByCPrivate);

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<List<BeingListModel>> GetBeingsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/beings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<List<BeingListModel>>();
    }

    [Fact]
    public async Task BeingList_UserA_ReturnsOwnAndConnectedBeingsOnly()
    {
        var client = factory.CreateClientForUser(UserAId, UserAName);
        var models = await GetBeingsAsync(client);
        var beingIds = models.Select(m => m.Id).ToList();
        var modelById = models.ToDictionary(m => m.Id);

        Assert.Contains(BeingOwnedByAInherited, beingIds);
        Assert.Contains(BeingSharedWithB, beingIds);
        Assert.Contains(BeingPrivateA, beingIds);
        Assert.Contains(BeingOwnedByBInherited, beingIds);

        Assert.DoesNotContain(BeingOwnedByCPrivate, beingIds);
        Assert.Equal(4, models.Count);
        Assert.Equal(1, modelById[BeingOwnedByAInherited].Entries);
        Assert.Equal(1, modelById[BeingSharedWithB].Entries);
        Assert.Equal(1, modelById[BeingPrivateA].Entries);
        Assert.Equal(1, modelById[BeingOwnedByBInherited].Entries);
    }

    [Fact]
    public async Task BeingList_UserB_ReturnsOwnAndAccessibleBeingsOnly()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var models = await GetBeingsAsync(client);
        var beingIds = models.Select(m => m.Id).ToList();
        var modelById = models.ToDictionary(m => m.Id);

        Assert.Contains(BeingOwnedByBInherited, beingIds);
        Assert.Contains(BeingOwnedByAInherited, beingIds);
        Assert.Contains(BeingSharedWithB, beingIds);

        Assert.DoesNotContain(BeingPrivateA, beingIds);
        Assert.DoesNotContain(BeingOwnedByCPrivate, beingIds);
        Assert.Equal(3, models.Count);
        Assert.Equal(1, modelById[BeingOwnedByBInherited].Entries);
        Assert.Equal(1, modelById[BeingOwnedByAInherited].Entries);
        Assert.Equal(0, modelById[BeingSharedWithB].Entries);
    }

    [Fact]
    public async Task BeingList_UserC_ReturnsOnlyOwnBeings()
    {
        var client = factory.CreateClientForUser(UserCId, UserCName);
        var models = await GetBeingsAsync(client);
        var beingIds = models.Select(m => m.Id).ToList();
        var modelById = models.ToDictionary(m => m.Id);

        Assert.Contains(BeingOwnedByCPrivate, beingIds);

        Assert.DoesNotContain(BeingOwnedByAInherited, beingIds);
        Assert.DoesNotContain(BeingSharedWithB, beingIds);
        Assert.DoesNotContain(BeingPrivateA, beingIds);
        Assert.DoesNotContain(BeingOwnedByBInherited, beingIds);
        Assert.Single(models);
        Assert.Equal(1, modelById[BeingOwnedByCPrivate].Entries);
    }

    [Fact]
    public async Task BeingList_Anonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/beings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
