using System.Net;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class BeingAccessTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string OwnerUserId = "b-owner-id";
    private const string OwnerUserName = "bowner";
    private const string ReaderUserId = "b-reader-id";
    private const string ReaderUserName = "breader";
    private const string CoOwnerUserId = "b-coowner-id";
    private const string CoOwnerUserName = "bcoowner";
    private const string StrangerUserId = "b-stranger-id";
    private const string StrangerUserName = "bstranger";

    private const string UserBeingId = "b-owner-id"; // Same as OwnerUserId (user being)
    private const string InheritedBeingId = "being-inh";
    private const string ExplicitBeingId = "being-exp";
    private const string PublicBeingId = "being-pub";

    public BeingAccessTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(BeingAccessTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, ReaderUserId, ReaderUserName);
            await DatabaseSeeder.SeedUser(accountsDb, CoOwnerUserId, CoOwnerUserName);
            await DatabaseSeeder.SeedUser(accountsDb, StrangerUserId, StrangerUserName);

            // owner grants Read to reader
            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, ReaderUserId,
                Permission.Read, Permission.Read);

            // owner grants CoOwner to coowner
            await DatabaseSeeder.SeedConnection(accountsDb, OwnerUserId, CoOwnerUserId,
                Permission.CoOwner, Permission.Read);

            // User being (Being.Id == Being.UserId) — auto-shared on connection
            await DatabaseSeeder.SeedUserBeing(entriesDb, OwnerUserId, OwnerUserName);
            await DatabaseSeeder.SeedBeingShare(entriesDb, UserBeingId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedBeingShare(entriesDb, UserBeingId, CoOwnerUserId, Permission.Read);

            // Regular being with inherited sharing (uses connections)
            await DatabaseSeeder.SeedBeing(entriesDb, InheritedBeingId, OwnerUserId, isSharingInherited: true);

            // Regular being with explicit sharing
            await DatabaseSeeder.SeedBeing(entriesDb, ExplicitBeingId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, ExplicitBeingId, ReaderUserId, Permission.Read);
            await DatabaseSeeder.SeedBeingShare(entriesDb, ExplicitBeingId, CoOwnerUserId, Permission.CoOwner);

            // Public being
            await DatabaseSeeder.SeedBeing(entriesDb, PublicBeingId, OwnerUserId, isSharingInherited: false);
            await DatabaseSeeder.SeedBeingShare(entriesDb, PublicBeingId, ShareStatusService.PublicUserId, Permission.Read);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<AuthorizedModel<BeingModel>> GetBeingAsync(HttpClient client, string beingId, params string[] forbiddenBeingIds)
    {
        var response = await client.GetAsync($"/api/beings/{beingId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.ReadJsonAsync<AuthorizedModel<BeingModel>>();
        Assert.Equal(beingId, result.Model.Id);
        foreach (var forbiddenBeingId in forbiddenBeingIds)
            Assert.NotEqual(forbiddenBeingId, result.Model.Id);

        return result;
    }

    // ===== User being (profile) =====

    [Fact]
    public async Task UserBeing_AsOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var result = await GetBeingAsync(client, UserBeingId, InheritedBeingId, ExplicitBeingId, PublicBeingId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task UserBeing_AsConnectedReader_ReturnsReadPermission()
    {
        // User being has explicit BeingShare for reader (auto-created on connection)
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetBeingAsync(client, UserBeingId, InheritedBeingId, ExplicitBeingId, PublicBeingId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task UserBeing_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/beings/{UserBeingId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== Inherited being =====

    [Fact]
    public async Task InheritedBeing_AsOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(OwnerUserId, OwnerUserName);
        var result = await GetBeingAsync(client, InheritedBeingId, UserBeingId, ExplicitBeingId, PublicBeingId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task InheritedBeing_AsConnectedReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetBeingAsync(client, InheritedBeingId, UserBeingId, ExplicitBeingId, PublicBeingId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task InheritedBeing_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/beings/{InheritedBeingId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InheritedBeing_AsAnonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync($"/api/beings/{InheritedBeingId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== Explicit being =====

    [Fact]
    public async Task ExplicitBeing_AsExplicitReader_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var result = await GetBeingAsync(client, ExplicitBeingId, UserBeingId, InheritedBeingId, PublicBeingId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task ExplicitBeing_AsExplicitCoOwner_ReturnsCoOwnerPermission()
    {
        var client = factory.CreateClientForUser(CoOwnerUserId, CoOwnerUserName);
        var result = await GetBeingAsync(client, ExplicitBeingId, UserBeingId, InheritedBeingId, PublicBeingId);
        Assert.Equal(Permission.CoOwner, result.UserPermission);
    }

    [Fact]
    public async Task ExplicitBeing_AsStranger_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var response = await client.GetAsync($"/api/beings/{ExplicitBeingId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExplicitBeing_UpdateAsReader_ReturnsUnauthorized()
    {
        var client = factory.CreateClientForUser(ReaderUserId, ReaderUserName);
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"/api/beings/{ExplicitBeingId}", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===== Public being =====

    [Fact]
    public async Task PublicBeing_AsAnonymous_ReturnsReadPermission()
    {
        var client = factory.CreateAnonymousClient();
        var result = await GetBeingAsync(client, PublicBeingId, UserBeingId, InheritedBeingId, ExplicitBeingId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }

    [Fact]
    public async Task PublicBeing_AsStranger_ReturnsReadPermission()
    {
        var client = factory.CreateClientForUser(StrangerUserId, StrangerUserName);
        var result = await GetBeingAsync(client, PublicBeingId, UserBeingId, InheritedBeingId, ExplicitBeingId);
        Assert.Equal(Permission.Read, result.UserPermission);
    }
}
