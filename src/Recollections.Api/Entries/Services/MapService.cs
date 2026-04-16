using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries;

public class MapService
{
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

    public async Task<List<MapEntryModel>> GetAsync(IQueryable<Entry> query, string userId, string[] userIds, ConnectedUsersModel connectedUsers)
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
        var locationById = new Dictionary<string, LocationModel>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var location = item.Location;

            if (!HasLocationValue(location))
                location = item.TrackLocation;

            if (!HasLocationValue(location))
            {
                location = await dataContext.Images
                    .Where(img => img.Entry.Id == item.Id && img.Location.Latitude != null && img.Location.Longitude != null)
                    .Select(img => new LocationModel()
                    {
                        Latitude = img.Location.Latitude,
                        Longitude = img.Location.Longitude,
                        Altitude = img.Location.Altitude
                    })
                    .FirstOrDefaultAsync();
            }

            if (!HasLocationValue(location))
            {
                location = await dataContext.Videos
                    .Where(v => v.Entry.Id == item.Id && v.Location.Latitude != null && v.Location.Longitude != null)
                    .Select(v => new LocationModel()
                    {
                        Latitude = v.Location.Latitude,
                        Longitude = v.Location.Longitude,
                        Altitude = v.Location.Altitude
                    })
                    .FirstOrDefaultAsync();
            }

            if (HasLocationValue(location))
                locationById[item.Id] = location;
        }

        if (locationById.Count == 0)
            return [];

        // Enrich with full entry list models
        var entryQuery = dataContext.Entries.Where(e => locationById.Keys.Contains(e.Id));
        var (entryModels, _) = await entryListMapper.MapAsync(entryQuery, userId, connectedUsers);

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
}
