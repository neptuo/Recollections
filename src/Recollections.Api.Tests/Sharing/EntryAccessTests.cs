using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class EntryAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    // Users
    private const string OwnerUserId = "owner-id";
    private const string OwnerUserName = "owner";
    private const string ReaderUserId = "reader-id";
    private const string ReaderUserName = "reader";
    private const string CoOwnerUserId = "coowner-id";
    private const string CoOwnerUserName = "coowner";
    private const string StrangerUserId = "stranger-id";
    private const string StrangerUserName = "stranger";
    private const string StoryOwnerUserId = "storyowner-id";
    private const string StoryOwnerUserName = "storyowner";

    // Entries
    private const string InheritedEntryId = "entry-inherited";
    private const string ExplicitEntryId = "entry-explicit";
    private const string PublicEntryId = "entry-public";
    private const string UpdatableInheritedEntryId = "entry-inherited-updatable";
    private const string StoryInheritedEntryId = "entry-story-inherited";
    private const string StoryCascadeEntryId = "entry-story-cascade";
    private const string StoryExplicitEntryId = "entry-story-explicit";

    // Stories
    private const string InheritedStoryId = "story-inherited";
    private const string ExplicitStoryId = "story-explicit";

    public EntryAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(EntryAccessTests), async (accountsDb, entriesDb) =>
        {
            // Create users
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, CoOwnerUserId, CoOwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StoryOwnerUserId, StoryOwnerUserName);

            // Connection: owner grants Read to reader
            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId,
                permission1: Permission.Read, permission2: Permission.Read);

            // Connection: owner grants CoOwner to coowner
            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, CoOwnerUserId,
                permission1: Permission.CoOwner, permission2: Permission.Read);

            // Connection: storyowner grants Read to reader
            await DatabaseSeeder.SeedConnection(accountsDb, StoryOwnerUserId, ReaderUserId,
                permission1: Permission.Read, permission2: Permission.Read);

            // No connection between owner and stranger

            // 1. Entry with inherited sharing (uses connections)
            await DatabaseSeeder.SeedEntry(entriesDb, InheritedEntryId, OwnerUserId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntry(entriesDb, UpdatableInheritedEntryId, OwnerUserId, isSharingInherited: true);

            // 2. Entry with explicit sharing
            await DatabaseSeeder.SeedEntry(entriesDb, ExplicitEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, ExplicitEntryId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedEntryShare(entriesDb, ExplicitEntryId, CoOwnerUserId, Permission.CoOwner);

            // 3. Public entry
            await DatabaseSeeder.SeedEntry(entriesDb, PublicEntryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedEntryShare(entriesDb, PublicEntryId, ShareStatusService.PublicUserId, Permission.Read);

            // 4. Entry in inherited story (story uses connections)
            var inheritedStory = await DatabaseSeeder.SeedStory(entriesDb, InheritedStoryId, StoryOwnerUserId, isSharingInherited: true);
            await DatabaseSeeder.SeedEntry(entriesDb, StoryInheritedEntryId, StoryOwnerUserId, isSharingInherited: true, story: inheritedStory);

            // 5. Entry inheriting from explicitly shared story
            var explicitStory = await DatabaseSeeder.SeedStory(entriesDb, ExplicitStoryId, StoryOwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, ExplicitStoryId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedEntry(entriesDb, StoryCascadeEntryId, StoryOwnerUserId, isSharingInherited: true, story: explicitStory);

            // 6. Entry with explicit sharing in explicit story (entry doesn't inherit)
            await DatabaseSeeder.SeedEntry(entriesDb, StoryExplicitEntryId, StoryOwnerUserId, isSharingInherited: false, story: explicitStory);
            await DatabaseSeeder.SeedEntryShare(entriesDb, StoryExplicitEntryId, CoOwnerUserId, Permission.CoOwner);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<AuthorizedModel<EntryModel>> GetEntryAsync(HttpClient client, string entryId, params string[] forbiddenEntryIds)
    {
        var response = await client.GetAsync($"/api/entries/{entryId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.ReadJsonAsync<AuthorizedModel<EntryModel>>();
        Assert.Equal(entryId, result.Model.Id);
        foreach (var forbiddenEntryId in forbiddenEntryIds)
            Assert.NotEqual(forbiddenEntryId, result.Model.Id);

        return result;
    }

    // ===== Inherited entry (uses connections) =====

    [Fact]
    public async Task InheritedEntry_AsOwner_ReturnsOkWithCorrectId()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var result = await GetEntryAsync(client, InheritedEntryId, ExplicitEntryId, PublicEntryId);
        Assert.Equal(OwnerUserId, result.OwnerId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task InheritedEntry_AsConnectedReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetEntryAsync(client, InheritedEntryId, ExplicitEntryId, PublicEntryId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task InheritedEntry_AsConnectedCoOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var result = await GetEntryAsync(client, InheritedEntryId, ExplicitEntryId, PublicEntryId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task InheritedEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{InheritedEntryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InheritedEntry_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/entries/{InheritedEntryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InheritedEntry_UpdateAsReader_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"/api/entries/{UpdatableInheritedEntryId}", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InheritedEntry_UpdateAsCoOwner_Succeeds()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var json = $"{{\"Id\":\"{UpdatableInheritedEntryId}\",\"Title\":\"Updated\",\"When\":\"2025-01-01T00:00:00\",\"Locations\":[]}}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"/api/entries/{UpdatableInheritedEntryId}", content);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ===== Explicit entry shares =====

    [Fact]
    public async Task ExplicitEntry_AsExplicitReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetEntryAsync(client, ExplicitEntryId, InheritedEntryId, PublicEntryId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task ExplicitEntry_AsExplicitCoOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var result = await GetEntryAsync(client, ExplicitEntryId, InheritedEntryId, PublicEntryId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task ExplicitEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{ExplicitEntryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExplicitEntry_UpdateAsReader_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"/api/entries/{ExplicitEntryId}", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== Public entry =====

    [Fact]
    public async Task PublicEntry_AsAnonymous_ReturnsReadPermission()
    {
        var client = factory.CreateAnonymousClient();
        var result = await GetEntryAsync(client, PublicEntryId, InheritedEntryId, ExplicitEntryId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task PublicEntry_AsStranger_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var result = await GetEntryAsync(client, PublicEntryId, InheritedEntryId, ExplicitEntryId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    // ===== Entry cascade through story =====

    [Fact]
    public async Task StoryCascadeEntry_AsStoryOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(StoryOwnerUserId, StoryOwnerUserName);
        var result = await GetEntryAsync(client, StoryCascadeEntryId, StoryExplicitEntryId, StoryInheritedEntryId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task StoryCascadeEntry_ViaExplicitStoryShare_ReturnsReadPermission()
    {
        // Reader has explicit StoryShare on the explicit story
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetEntryAsync(client, StoryCascadeEntryId, StoryExplicitEntryId, StoryInheritedEntryId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task StoryCascadeEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{StoryCascadeEntryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StoryInheritedEntry_ViaConnection_ReturnsReadPermission()
    {
        // Reader is connected to storyowner with Read permission
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetEntryAsync(client, StoryInheritedEntryId, StoryCascadeEntryId, StoryExplicitEntryId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task StoryInheritedEntry_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/entries/{StoryInheritedEntryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== Entry with own explicit share in a story =====

    [Fact]
    public async Task StoryExplicitEntry_AsExplicitCoOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var result = await GetEntryAsync(client, StoryExplicitEntryId, StoryCascadeEntryId, StoryInheritedEntryId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task StoryExplicitEntry_AsStoryShareReader_ReturnsUnauthorized()
    {
        // Reader has StoryShare on the story, but entry has IsSharingInherited=false
        // and no EntryShare for reader — so no access
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var response = await client.GetAsync($"/api/entries/{StoryExplicitEntryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
