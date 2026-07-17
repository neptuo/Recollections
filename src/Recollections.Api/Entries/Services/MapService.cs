using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries;

public class MapService
{
    private const int QueryBatchSize = 250;

    private readonly DataContext dataContext;
    private readonly ShareStatusService shareStatus;
    private readonly EntryListMapper entryListMapper;

    public MapService(DataContext dataContext, ShareStatusService shareStatus, EntryListMapper entryListMapper)
    {
        Ensure.NotNull(dataContext, "dataContext");
        Ensure.NotNull(shareStatus, "shareStatus");
        Ensure.NotNull(entryListMapper, "entryListMapper");
        this.dataContext = dataContext;
        this.shareStatus = shareStatus;
        this.entryListMapper = entryListMapper;
    }

    public async Task<List<MapEntryModel>> GetAsync(IQueryable<Entry> query, string[] userIds, ConnectedUsersModel connectedUsers)
    {
        var items = await shareStatus
            .OwnedByOrExplicitlySharedWithUser(dataContext, query, userIds, connectedUsers)
            .Select(e => new
            {
                Id = e.Id,
                Location = e.Locations
                    .Where(l => l.Latitude != null && l.Longitude != null)
                    .Select(l => new LocationModel()
                    {
                        Latitude = l.Latitude,
                        Longitude = l.Longitude,
                        Altitude = l.Altitude
                    })
                    .FirstOrDefault(),
                TrackLocation = e.TrackLatitude != null && e.TrackLongitude != null
                    ? new LocationModel()
                    {
                        Latitude = e.TrackLatitude,
                        Longitude = e.TrackLongitude,
                        Altitude = e.TrackAltitude
                    }
                    : null
            })
            .ToListAsync();

        // Resolve locations with fallback chain: entry location → track → image → video
        var locationById = new Dictionary<string, LocationModel>(items.Count);
        var unresolvedEntryIds = new List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var location = item.Location;

            if (!HasLocationValue(location))
                location = item.TrackLocation;

            if (HasLocationValue(location))
                locationById[item.Id] = location;
            else
                unresolvedEntryIds.Add(item.Id);
        }

        if (unresolvedEntryIds.Count > 0)
        {
            foreach (List<string> batch in Batch(unresolvedEntryIds, QueryBatchSize))
            {
                var imageLocations = await dataContext.Images
                    .Where(img =>
                        batch.Contains(img.Entry.Id)
                        && img.Location != null
                        && img.Location.Latitude != null
                        && img.Location.Longitude != null
                    )
                    .Select(img => new
                    {
                        EntryId = img.Entry.Id,
                        Location = new LocationModel()
                        {
                            Latitude = img.Location.Latitude,
                            Longitude = img.Location.Longitude,
                            Altitude = img.Location.Altitude
                        }
                    })
                    .ToListAsync();

                foreach (var imageLocation in imageLocations)
                {
                    if (!locationById.ContainsKey(imageLocation.EntryId))
                        locationById[imageLocation.EntryId] = imageLocation.Location;
                }
            }
        }

        if (locationById.Count > 0 && unresolvedEntryIds.Count > 0)
        {
            unresolvedEntryIds = unresolvedEntryIds
                .Where(entryId => !locationById.ContainsKey(entryId))
                .ToList();
        }

        if (unresolvedEntryIds.Count > 0)
        {
            foreach (List<string> batch in Batch(unresolvedEntryIds, QueryBatchSize))
            {
                var videoLocations = await dataContext.Videos
                    .Where(video =>
                        batch.Contains(video.Entry.Id)
                        && video.Location != null
                        && video.Location.Latitude != null
                        && video.Location.Longitude != null
                    )
                    .Select(video => new
                    {
                        EntryId = video.Entry.Id,
                        Location = new LocationModel()
                        {
                            Latitude = video.Location.Latitude,
                            Longitude = video.Location.Longitude,
                            Altitude = video.Location.Altitude
                        }
                    })
                    .ToListAsync();

                foreach (var videoLocation in videoLocations)
                {
                    if (!locationById.ContainsKey(videoLocation.EntryId))
                        locationById[videoLocation.EntryId] = videoLocation.Location;
                }
            }
        }

        if (locationById.Count == 0)
            return [];

        // Enrich with full entry list models
        var entryIds = locationById.Keys.ToList();
        var entryModels = new List<EntryListModel>(entryIds.Count);
        foreach (List<string> batch in Batch(entryIds, QueryBatchSize))
        {
            var entryQuery = dataContext.Entries.Where(e => batch.Contains(e.Id));
            var (batchModels, _) = await entryListMapper.MapAsync(entryQuery, userIds, connectedUsers);
            entryModels.AddRange(batchModels);
        }

        var results = new List<MapEntryModel>(entryModels.Count);
        foreach (var entry in entryModels)
        {
            if (locationById.TryGetValue(entry.Id, out var location))
            {
                results.Add(new MapEntryModel()
                {
                    Location = location,
                    Entry = entry
                });
            }
        }

        return results;
    }

    private static bool HasLocationValue(LocationModel location) => location != null && location.HasValue();

    private static IEnumerable<List<string>> Batch(List<string> values, int size)
    {
        for (int i = 0; i < values.Count; i += size)
            yield return values.Skip(i).Take(size).ToList();
    }
}
