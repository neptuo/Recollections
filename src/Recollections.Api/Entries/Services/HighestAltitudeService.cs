using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries;

public class HighestAltitudeService(DataContext dataContext, EntryListMapper entryMapper, ShareStatusService shareStatus)
{
    public const int ItemCount = 20;

    public async Task<List<EntryListModel>> GetListAsync(string userId, ConnectedUsersModel connectedUsers)
    {
        Ensure.NotNullOrEmpty(userId, "userId");
        Ensure.NotNull(connectedUsers, "connectedUsers");

        IQueryable<Entry> accessibleEntries = shareStatus
            .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries.AsNoTracking(), userId, connectedUsers);

        List<EntryAltitudeInfo> rankedEntries = await GetRankedEntriesAsync(accessibleEntries);
        if (rankedEntries.Count == 0)
            return [];

        HashSet<string> entryIds = rankedEntries
            .Select(item => item.EntryId)
            .ToHashSet();

        var query = shareStatus.OwnedByOrExplicitlySharedWithUser(
            dataContext,
            dataContext.Entries.Where(e => entryIds.Contains(e.Id)),
            userId,
            connectedUsers
        );

        var (models, _) = await entryMapper.MapAsync(query, userId, connectedUsers, includePreviewMedia: true);
        Dictionary<string, EntryListModel> modelsById = models.ToDictionary(model => model.Id);

        return rankedEntries
            .Where(item => modelsById.ContainsKey(item.EntryId))
            .Select(item => modelsById[item.EntryId] with { Altitude = item.Altitude })
            .ToList();
    }

    private async Task<List<EntryAltitudeInfo>> GetRankedEntriesAsync(IQueryable<Entry> accessibleEntries)
    {
        IQueryable<string> accessibleEntryIds = accessibleEntries.Select(e => e.Id);
        var altitudeSources = accessibleEntries
            .SelectMany(
                e => e.Locations.Where(l => l.Altitude != null),
                (e, location) => new
                {
                    EntryId = e.Id,
                    e.When,
                    Altitude = location.Altitude!.Value
                }
            )
            .Concat(dataContext.Images
                .AsNoTracking()
                .Where(i => accessibleEntryIds.Contains(i.Entry.Id) && i.Location.Altitude != null)
                .Select(i => new
                {
                    EntryId = i.Entry.Id,
                    When = i.Entry.When,
                    Altitude = i.Location.Altitude!.Value
                }))
            .Concat(dataContext.Videos
                .AsNoTracking()
                .Where(v => accessibleEntryIds.Contains(v.Entry.Id) && v.Location.Altitude != null)
                .Select(v => new
                {
                    EntryId = v.Entry.Id,
                    When = v.Entry.When,
                    Altitude = v.Location.Altitude!.Value
                }));

        var rankedEntries = await altitudeSources
            .GroupBy(item => item.EntryId)
            .Select(group => new
            {
                EntryId = group.Key,
                Altitude = group.Max(item => item.Altitude),
                When = group.Max(item => item.When)
            })
            .OrderByDescending(item => item.Altitude)
            .ThenByDescending(item => item.When)
            .ThenBy(item => item.EntryId)
            .Take(ItemCount)
            .ToListAsync();

        return rankedEntries
            .Select(item => new EntryAltitudeInfo(item.EntryId, item.Altitude, item.When))
            .ToList();
    }

    private sealed record EntryAltitudeInfo(string EntryId, double Altitude, DateTime When);
}
