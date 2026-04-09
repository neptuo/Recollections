using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class EntryBeingAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string JonUserId = "eb-jon-id";
    private const string JonUserName = "ebjon";
    private const string JaneUserId = "eb-jane-id";
    private const string JaneUserName = "ebjane";

    private const string SharedEntryId = "eb-shared-entry";
    private const string AliceBeingId = "eb-being-alice";
    private const string PeterBeingId = "eb-being-peter";

    public EntryBeingAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryBeingAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, JonUserId, JonUserName);
            await DatabaseSeeder.SeedUser(accountsDb, JaneUserId, JaneUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, JonUserId, JaneUserId, permission1: null, permission2: null);

            var alice = await DatabaseSeeder.SeedBeing(entriesDb, AliceBeingId, JonUserId, isSharingInherited: false);
            var peter = await DatabaseSeeder.SeedBeing(entriesDb, PeterBeingId, JonUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, AliceBeingId, JaneUserId, Permission.Read);

            var entry = await DatabaseSeeder.SeedEntry(entriesDb, SharedEntryId, JonUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, SharedEntryId, JaneUserId, Permission.Read);

            entry.Beings.Add(alice);
            entry.Beings.Add(peter);
            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<List<EntryBeingModel>> GetEntryBeingsAsync(HttpClient client, string entryId)
    {
        var response = await client.GetAsync($"/api/entries/{entryId}/beings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<List<EntryBeingModel>>();
    }

    [Fact]
    public async Task ExplicitlySharedEntry_ReturnsOnlyAttachedBeingsVisibleToTheReader()
    {
        var client = factory.CreateClientForUser(JaneUserId, JaneUserName);
        var models = await GetEntryBeingsAsync(client, SharedEntryId);
        var beingIds = models.Select(model => model.Id).ToList();

        Assert.Contains(AliceBeingId, beingIds);
        Assert.DoesNotContain(PeterBeingId, beingIds);
        Assert.Single(models);
    }
}
