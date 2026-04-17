using System.Net;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using Neptuo.Recollections.Tests.Infrastructure;
using Xunit;

namespace Neptuo.Recollections.Tests.Sharing;

public class BeingHighestAltitudeViewTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory factory;

    private const string UserAId = "bha-usera-id";
    private const string UserAName = "bhausera";
    private const string UserBId = "bha-userb-id";
    private const string UserBName = "bhauserb";

    private const string BeingId = "bha-being";
    private const string OtherBeingId = "bha-other-being";

    private const string HighAltitudeEntryId = "bha-entry-high";
    private const string MediumAltitudeEntryId = "bha-entry-medium";
    private const string LowAltitudeEntryId = "bha-entry-low";
    private const string OtherBeingEntryId = "bha-entry-other-being";
    private const string NoBeingEntryId = "bha-entry-no-being";
    private const string HiddenEntryId = "bha-entry-hidden";
    private const string TrackAltitudeEntryId = "bha-entry-track";

    public BeingHighestAltitudeViewTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        await factory.SeedAsync(nameof(BeingHighestAltitudeViewTests), async (accountsDb, entriesDb) =>
        {
            await DatabaseSeeder.SeedUser(accountsDb, UserAId, UserAName);
            await DatabaseSeeder.SeedUser(accountsDb, UserBId, UserBName);
            await DatabaseSeeder.SeedConnection(accountsDb, UserAId, UserBId, Permission.Read, Permission.Read);

            var being = await DatabaseSeeder.SeedBeing(entriesDb, BeingId, UserAId, isSharingInherited: true);
            var otherBeing = await DatabaseSeeder.SeedBeing(entriesDb, OtherBeingId, UserAId, isSharingInherited: true);

            // Entry with high altitude, tagged with being
            var highEntry = await DatabaseSeeder.SeedEntry(
                entriesDb, HighAltitudeEntryId, UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, highEntry, 50.087, 14.421, altitude: 2000);
            highEntry.Beings.Add(being);

            // Entry with medium altitude, tagged with being
            var mediumEntry = await DatabaseSeeder.SeedEntry(
                entriesDb, MediumAltitudeEntryId, UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 2, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, mediumEntry, 50.088, 14.422, altitude: 500);
            mediumEntry.Beings.Add(being);

            // Entry with low altitude, tagged with being
            var lowEntry = await DatabaseSeeder.SeedEntry(
                entriesDb, LowAltitudeEntryId, UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 3, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, lowEntry, 50.089, 14.423, altitude: 100);
            lowEntry.Beings.Add(being);

            // Entry with altitude tagged with OTHER being (should not appear)
            var otherBeingEntry = await DatabaseSeeder.SeedEntry(
                entriesDb, OtherBeingEntryId, UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 4, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, otherBeingEntry, 49.195, 16.608, altitude: 3000);
            otherBeingEntry.Beings.Add(otherBeing);

            // Entry with altitude but NO being (should not appear)
            var noBeingEntry = await DatabaseSeeder.SeedEntry(
                entriesDb, NoBeingEntryId, UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 5, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, noBeingEntry, 48.149, 17.107, altitude: 4000);

            // Entry tagged with being but hidden (not shared)
            var hiddenEntry = await DatabaseSeeder.SeedEntry(
                entriesDb, HiddenEntryId, UserAId,
                isSharingInherited: false,
                when: new DateTime(2024, 1, 6, 8, 0, 0, DateTimeKind.Utc));
            await DatabaseSeeder.SeedEntryLocation(entriesDb, hiddenEntry, 47.0, 15.0, altitude: 5000);
            hiddenEntry.Beings.Add(being);

            // Entry tagged with being, altitude from track only
            var trackEntry = await DatabaseSeeder.SeedEntry(
                entriesDb, TrackAltitudeEntryId, UserAId,
                isSharingInherited: true,
                when: new DateTime(2024, 1, 7, 8, 0, 0, DateTimeKind.Utc));
            trackEntry.TrackAltitude = 1500;
            trackEntry.Beings.Add(being);

            await entriesDb.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<List<EntryListModel>> GetBeingHighestAltitudeAsync(HttpClient client, string beingId)
    {
        var response = await client.GetAsync($"/api/beings/{beingId}/highest-altitude");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync<List<EntryListModel>>();
    }

    [Fact]
    public async Task BeingHighestAltitude_ReturnsOnlyEntriesForSpecificBeing()
    {
        var client = factory.CreateClientForUser(UserAId, UserAName);
        var models = await GetBeingHighestAltitudeAsync(client, BeingId);

        var entryIds = models.Select(m => m.Id).ToList();
        Assert.DoesNotContain(OtherBeingEntryId, entryIds);
        Assert.DoesNotContain(NoBeingEntryId, entryIds);
    }

    [Fact]
    public async Task BeingHighestAltitude_ReturnsEntriesInDescendingAltitudeOrder()
    {
        var client = factory.CreateClientForUser(UserAId, UserAName);
        var models = await GetBeingHighestAltitudeAsync(client, BeingId);

        Assert.True(models.Count >= 3);

        var highIdx = models.FindIndex(m => m.Id == HighAltitudeEntryId);
        var mediumIdx = models.FindIndex(m => m.Id == MediumAltitudeEntryId);
        var lowIdx = models.FindIndex(m => m.Id == LowAltitudeEntryId);

        Assert.True(highIdx < mediumIdx, "High altitude entry should come before medium");
        Assert.True(mediumIdx < lowIdx, "Medium altitude entry should come before low");
    }

    [Fact]
    public async Task BeingHighestAltitude_IncludesTrackAltitude()
    {
        var client = factory.CreateClientForUser(UserAId, UserAName);
        var models = await GetBeingHighestAltitudeAsync(client, BeingId);

        var trackEntry = models.FirstOrDefault(m => m.Id == TrackAltitudeEntryId);
        Assert.NotNull(trackEntry);
        Assert.Equal(1500, trackEntry.Altitude);
    }

    [Fact]
    public async Task BeingHighestAltitude_UserB_RespectsAccessControl()
    {
        var client = factory.CreateClientForUser(UserBId, UserBName);
        var models = await GetBeingHighestAltitudeAsync(client, BeingId);

        var entryIds = models.Select(m => m.Id).ToList();
        Assert.Contains(HighAltitudeEntryId, entryIds);
        Assert.Contains(MediumAltitudeEntryId, entryIds);
        Assert.Contains(LowAltitudeEntryId, entryIds);
        Assert.Contains(TrackAltitudeEntryId, entryIds);
        Assert.DoesNotContain(HiddenEntryId, entryIds);
    }

    [Fact]
    public async Task BeingHighestAltitude_SetsAltitudeOnModels()
    {
        var client = factory.CreateClientForUser(UserAId, UserAName);
        var models = await GetBeingHighestAltitudeAsync(client, BeingId);

        var highEntry = models.First(m => m.Id == HighAltitudeEntryId);
        Assert.Equal(2000, highEntry.Altitude);

        var mediumEntry = models.First(m => m.Id == MediumAltitudeEntryId);
        Assert.Equal(500, mediumEntry.Altitude);
    }
}
