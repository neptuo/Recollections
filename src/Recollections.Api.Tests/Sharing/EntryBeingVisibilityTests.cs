using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class EntryBeingVisibilityTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "ebv-owner-id";
    private const string OwnerUserName = "ebvowner";
    private const string ReaderUserId = "ebv-reader-id";
    private const string ReaderUserName = "ebvreader";
    private const string StrangerUserId = "ebv-stranger-id";
    private const string StrangerUserName = "ebvstranger";

    private const string PrivateEntryId = "ebv-entry-private";
    private const string PublicEntryId = "ebv-entry-public";
    private const string InheritedEntryId = "ebv-entry-inherited";

    private const string PublicBeingId = "ebv-being-public";
    private const string PrivateBeingId = "ebv-being-private";
    private const string InheritedBeingId = "ebv-being-inherited";

    public EntryBeingVisibilityTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryBeingVisibilityTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId,
                permission1: Permission.Read, permission2: Permission.Read);

            var publicBeing = await DatabaseSeeder.SeedBeing(entriesDb, PublicBeingId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, PublicBeingId, ShareStatusService.PublicUserId, Permission.Read);

            var privateBeing = await DatabaseSeeder.SeedBeing(entriesDb, PrivateBeingId, OwnerUserId, isSharingInherited: false);
            var inheritedBeing = await DatabaseSeeder.SeedBeing(entriesDb, InheritedBeingId, OwnerUserId, isSharingInherited: true);

            // Private entry with two attached beings
            var privateEntry = await DatabaseSeeder.SeedEntry(entriesDb, PrivateEntryId, OwnerUserId, isSharingInherited: false);
            privateEntry.Beings.Add(publicBeing);
            privateEntry.Beings.Add(privateBeing);

            // Public entry with all three beings attached
            var publicEntry = await DatabaseSeeder.SeedEntry(entriesDb, PublicEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicEntryId, ShareStatusService.PublicUserId, Permission.Read);
            publicEntry.Beings.Add(publicBeing);
            publicEntry.Beings.Add(privateBeing);
            publicEntry.Beings.Add(inheritedBeing);

            // Inherited entry (reader connected to owner) with two beings
            var inheritedEntry = await DatabaseSeeder.SeedEntry(entriesDb, InheritedEntryId, OwnerUserId, isSharingInherited: true);
            inheritedEntry.Beings.Add(inheritedBeing);
            inheritedEntry.Beings.Add(privateBeing);

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PrivateEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/beings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PrivateEntry_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PrivateEntryId}/beings");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicEntry_AsAnonymous_ReturnsOnlyPublicShareableBeings()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/beings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryBeingModel>>();
        var beingIds = models.Select(m => m.Id).ToList();

        Assert.Contains(PublicBeingId, beingIds);
        Assert.DoesNotContain(PrivateBeingId, beingIds);
        Assert.DoesNotContain(InheritedBeingId, beingIds);
        Assert.Single(models);
    }

    [Fact]
    public async Task InheritedEntry_AsConnectedReader_ReturnsBeingsVisibleViaConnection()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{InheritedEntryId}/beings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryBeingModel>>();
        var beingIds = models.Select(m => m.Id).ToList();

        Assert.Contains(InheritedBeingId, beingIds);
        Assert.DoesNotContain(PrivateBeingId, beingIds);
        Assert.Single(models);
    }

    [Fact]
    public async Task PublicEntry_AsOwner_ReturnsAllAttachedBeings()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var response = await client.GetAsync($"/api/entries/{PublicEntryId}/beings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var models = await response.ReadJsonAsync<List<EntryBeingModel>>();
        var beingIds = models.Select(m => m.Id).ToList();

        Assert.Contains(PublicBeingId, beingIds);
        Assert.Contains(PrivateBeingId, beingIds);
        Assert.Contains(InheritedBeingId, beingIds);
        Assert.Equal(3, models.Count);
    }
}
