using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class HighestAltitudeViewTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "ha-usera-id";
    private const string UserAName = "hausera";
    private const string UserBId = "ha-userb-id";
    private const string UserBName = "hauserb";
    private const string UserCId = "ha-userc-id";
    private const string UserCName = "hauserc";

    private const string HighestSharedEntryId = "ha-entry-high-shared";
    private const string ExplicitSharedEntryId = "ha-entry-explicit-shared";
    private const string MultiSourceEntryId = "ha-entry-multi-source";
    private const string OwnedByBEntryId = "ha-entry-owned-by-b";
    private const string HiddenPrivateEntryId = "ha-entry-private-hidden";

    public HighestAltitudeViewTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(HighestAltitudeViewTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedUser(accountsDb, UserCId, UserCName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            var highestShared = await DatabaseSeeder.SeedEntry(
                entriesDb,
                HighestSharedEntryId,
                UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, highestShared, 50.087, 14.421, altitude: 500);

            var explicitShared = await DatabaseSeeder.SeedEntry(
                entriesDb,
                ExplicitSharedEntryId,
                UserAId,
                isSharingInherited: false,
                when: new DateTime(2024, 1, 2, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryShare(entriesDb, ExplicitSharedEntryId, UserBId, Permission.Read);
            await DatabaseSeeder.SeedImage(entriesDb, "ha-image-explicit", explicitShared, latitude: 50.087, longitude: 14.421, altitude: 900);

            var multiSource = await DatabaseSeeder.SeedEntry(
                entriesDb,
                MultiSourceEntryId,
                UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 3, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, multiSource, 50.087, 14.421, altitude: 100);
            await DatabaseSeeder.SeedEntryLocation(entriesDb, multiSource, 50.088, 14.422, altitude: 1200);
            await DatabaseSeeder.SeedImage(entriesDb, "ha-image-multi", multiSource, latitude: 50.089, longitude: 14.423, altitude: 800);

            var ownedByB = await DatabaseSeeder.SeedEntry(
                entriesDb,
                OwnedByBEntryId,
                UserBId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 4, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, ownedByB, 49.195, 16.608, altitude: 700);

            var hiddenPrivate = await DatabaseSeeder.SeedEntry(
                entriesDb,
                HiddenPrivateEntryId,
                UserAId,
                isSharingInherited: false,
                when: new DateTime(2024, 1, 5, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, hiddenPrivate, 48.149, 17.107, altitude: 3000);

            for (int i = 1; i <= 25; i++)
            {
                var extra = await DatabaseSeeder.SeedEntry(
                    entriesDb,
                    $"ha-extra-{i:D2}",
                    UserAId,
                    isSharingInherited: true,
                    when: new DateTime(2024, 2, i, 8, 0, 0, DateTimeKind.Utc));
                await DatabaseSeeder.SeedEntryLocation(entriesDb, extra, 50 + i / 100.0, 14 + i / 100.0, altitude: i);
            }
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<List<EntryListModel>> GetHighestAltitudeAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/highest-altitude");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<List<EntryListModel>>();
    }

    [Fact]
    public async Task HighestAltitude_UserB_ReturnsAccessibleEntriesInDescendingOrder()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var models = await GetHighestAltitudeAsync(client);

        Assert.Equal(20, models.Count);
        Assert.Equal(MultiSourceEntryId, models[0].Id);
        Assert.Equal(1200, models[0].Altitude);
        Assert.Equal(ExplicitSharedEntryId, models[1].Id);
        Assert.Equal(900, models[1].Altitude);
        Assert.Equal(OwnedByBEntryId, models[2].Id);
        Assert.Equal(700, models[2].Altitude);
        Assert.Equal(HighestSharedEntryId, models[3].Id);
        Assert.Equal(500, models[3].Altitude);
    }

    [Fact]
    public async Task HighestAltitude_UsesHighestKnownAltitudeAndReturnsEntryOnlyOnce()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var models = await GetHighestAltitudeAsync(client);

        var model = Assert.Single(models.Where(m => m.Id == MultiSourceEntryId));
        Assert.Equal(1200, model.Altitude);
    }

    [Fact]
    public async Task HighestAltitude_ReturnsOnlyTopTwentyAccessibleItems()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var models = await GetHighestAltitudeAsync(client);
        var entryIds = models.Select(model => model.Id).ToList();

        Assert.Equal(20, entryIds.Count);
        Assert.DoesNotContain(HiddenPrivateEntryId, entryIds);
        Assert.Contains("ha-extra-25", entryIds);
        Assert.Contains("ha-extra-10", entryIds);
        Assert.DoesNotContain("ha-extra-09", entryIds);
        Assert.DoesNotContain("ha-extra-01", entryIds);
    }

    [Fact]
    public async Task HighestAltitude_Anonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.GetAsync("/api/highest-altitude");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
