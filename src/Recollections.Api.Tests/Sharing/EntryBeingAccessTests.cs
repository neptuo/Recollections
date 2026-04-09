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
    private const string EditorUserId = "eb-editor-id";
    private const string EditorUserName = "ebeditor";

    private const string SharedEntryId = "eb-shared-entry";
    private const string EditableEntryId = "eb-editable-entry";
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
            await DatabaseSeeder.SeedUser(accountsDb, EditorUserId, EditorUserName);

            await DatabaseSeeder.SeedConnection(accountsDb, JonUserId, JaneUserId, permission1: null, permission2: null);
            await DatabaseSeeder.SeedConnection(accountsDb, JonUserId, EditorUserId, permission1: null, permission2: null);

            var alice = await DatabaseSeeder.SeedBeing(entriesDb, AliceBeingId, JonUserId, isSharingInherited: false);
            var peter = await DatabaseSeeder.SeedBeing(entriesDb, PeterBeingId, JonUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, AliceBeingId, JaneUserId, Permission.Read);
            await DatabaseSeeder.SeedBeingShare(entriesDb, AliceBeingId, EditorUserId, Permission.Read);

            var entry = await DatabaseSeeder.SeedEntry(entriesDb, SharedEntryId, JonUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, SharedEntryId, JaneUserId, Permission.Read);

            entry.Beings.Add(alice);
            entry.Beings.Add(peter);

            await DatabaseSeeder.SeedEntry(entriesDb, EditableEntryId, JonUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, EditableEntryId, EditorUserId, Permission.CoOwner);

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

    [Fact]
    public async Task UpdatingAttachedBeings_IgnoresBeingsTheEditorCannotAccess()
    {
        var editorClient = factory.CreateClientForUser(EditorUserId, EditorUserName);
        var content = new StringContent(
            $"[\"{AliceBeingId}\",\"{PeterBeingId}\"]",
            System.Text.Encoding.UTF8,
            "application/json");

        var updateResponse = await editorClient.PutAsync($"/api/entries/{EditableEntryId}/beings", content);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var ownerClient = factory.CreateClientForUser(JonUserId, JonUserName);
        var models = await GetEntryBeingsAsync(ownerClient, EditableEntryId);
        var beingIds = models.Select(model => model.Id).ToList();

        Assert.Contains(AliceBeingId, beingIds);
        Assert.DoesNotContain(PeterBeingId, beingIds);
        Assert.Single(models);
    }
}
