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

    public async Task<List<HighestAltitudeEntryListModel>> GetListAsync(string userId, ConnectedUsersModel connectedUsers)
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
            .Select(item => MapModel(modelsById[item.EntryId], item.Altitude))
            .ToList();
    }

    private static HighestAltitudeEntryListModel MapModel(EntryListModel model, double altitude)
        => new(
            UserId: model.UserId,
            UserName: model.UserName,
            Id: model.Id,
            Title: model.Title,
            TextWordCount: model.TextWordCount,
            When: model.When,
            StoryTitle: model.StoryTitle,
            ChapterTitle: model.ChapterTitle,
            Beings: model.Beings,
            ImageCount: model.ImageCount,
            VideoCount: model.VideoCount,
            GpsCount: model.GpsCount,
            Altitude: altitude
        )
        {
            PreviewMedia = model.PreviewMedia
        };

    private async Task<List<EntryAltitudeInfo>> GetRankedEntriesAsync(IQueryable<Entry> accessibleEntries)
    {
        IQueryable<string> accessibleEntryIds = accessibleEntries.Select(e => e.Id);
        List<EntryAltitudeSource> altitudeSources = [];

        altitudeSources.AddRange(await accessibleEntries
            .SelectMany(
                e => e.Locations.Where(l => l.Altitude != null),
                (e, location) => new EntryAltitudeSource(e.Id, e.When, location.Altitude!.Value)
            )
            .ToListAsync());

        altitudeSources.AddRange(await dataContext.Images
            .AsNoTracking()
            .Where(i => accessibleEntryIds.Contains(i.Entry.Id) && i.Location.Altitude != null)
            .Select(i => new EntryAltitudeSource(i.Entry.Id, i.Entry.When, i.Location.Altitude!.Value))
            .ToListAsync());

        altitudeSources.AddRange(await dataContext.Videos
            .AsNoTracking()
            .Where(v => accessibleEntryIds.Contains(v.Entry.Id) && v.Location.Altitude != null)
            .Select(v => new EntryAltitudeSource(v.Entry.Id, v.Entry.When, v.Location.Altitude!.Value))
            .ToListAsync());

        return altitudeSources
            .GroupBy(item => item.EntryId)
            .Select(group => new EntryAltitudeInfo(
                EntryId: group.Key,
                Altitude: group.Max(item => item.Altitude),
                When: group.Max(item => item.When)
            ))
            .OrderByDescending(item => item.Altitude)
            .ThenByDescending(item => item.When)
            .ThenBy(item => item.EntryId)
            .Take(ItemCount)
            .ToList();
    }

    private sealed record EntryAltitudeSource(string EntryId, DateTime When, double Altitude);
    private sealed record EntryAltitudeInfo(string EntryId, double Altitude, DateTime When);
}
