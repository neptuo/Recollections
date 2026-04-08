using System.Net;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class StoryAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "s-owner-id";
    private const string OwnerUserName = "sowner";
    private const string ReaderUserId = "s-reader-id";
    private const string ReaderUserName = "sreader";
    private const string CoOwnerUserId = "s-coowner-id";
    private const string CoOwnerUserName = "scoowner";
    private const string StrangerUserId = "s-stranger-id";
    private const string StrangerUserName = "sstranger";

    private const string InheritedStoryId = "story-inh";
    private const string ExplicitStoryId = "story-exp";
    private const string PublicStoryId = "story-pub";

    public StoryAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(StoryAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, CoOwnerUserId, CoOwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            // owner grants Read to reader via connection
            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId,
                Permission.Read, Permission.Read);

            // owner grants CoOwner to coowner via connection
            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, CoOwnerUserId,
                Permission.CoOwner, Permission.Read);

            // Inherited story (uses connections)
            await DatabaseSeeder.SeedStory(entriesDb, InheritedStoryId, OwnerUserId, isSharingInherited: true);

            // Explicit story with shares
            await DatabaseSeeder.SeedStory(entriesDb, ExplicitStoryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, ExplicitStoryId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedStoryShare(entriesDb, ExplicitStoryId, CoOwnerUserId, Permission.CoOwner);

            // Public story
            await DatabaseSeeder.SeedStory(entriesDb, PublicStoryId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedStoryShare(entriesDb, PublicStoryId, ShareStatusService.PublicUserId, Permission.Read);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<AuthorizedModel<StoryModel>> GetStoryAsync(HttpClient client, string storyId)
    {
        var response = await client.GetAsync($"/api/stories/{storyId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<AuthorizedModel<StoryModel>>();
    }

    // ===== Inherited story =====

    [Fact]
    public async Task InheritedStory_AsOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var result = await GetStoryAsync(client, InheritedStoryId);

        Assert.Equal(InheritedStoryId, result.Model.Id);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task InheritedStory_AsConnectedReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetStoryAsync(client, InheritedStoryId);

        Assert.Equal(InheritedStoryId, result.Model.Id);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task InheritedStory_AsConnectedCoOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var result = await GetStoryAsync(client, InheritedStoryId);

        Assert.Equal(InheritedStoryId, result.Model.Id);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task InheritedStory_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/stories/{InheritedStoryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InheritedStory_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/stories/{InheritedStoryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InheritedStory_UpdateAsReader_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"/api/stories/{InheritedStoryId}", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== Explicit story =====

    [Fact]
    public async Task ExplicitStory_AsExplicitReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetStoryAsync(client, ExplicitStoryId);

        Assert.Equal(ExplicitStoryId, result.Model.Id);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task ExplicitStory_AsExplicitCoOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var result = await GetStoryAsync(client, ExplicitStoryId);

        Assert.Equal(ExplicitStoryId, result.Model.Id);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task ExplicitStory_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/stories/{ExplicitStoryId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== Public story =====

    [Fact]
    public async Task PublicStory_AsAnonymous_ReturnsReadPermission()
    {
        var client = factory.CreateAnonymousClient();
        var result = await GetStoryAsync(client, PublicStoryId);

        Assert.Equal(PublicStoryId, result.Model.Id);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task PublicStory_AsStranger_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var result = await GetStoryAsync(client, PublicStoryId);

        Assert.Equal(PublicStoryId, result.Model.Id);
        Assert.Equal(Permission.Read, result.UserPermission);
    }
}
