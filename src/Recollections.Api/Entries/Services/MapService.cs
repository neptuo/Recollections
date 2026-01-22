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

    public MapService(DataContext dataContext, ShareStatusService shareStatus)
    {
        Ensure.NotNull(dataContext, "dataContext");
        Ensure.NotNull(shareStatus, "shareStatus");
        this.dataContext = dataContext;
        this.shareStatus = shareStatus;
    }

    public async Task<List<MapEntryModel>> GetAsync(IQueryable<Entry> query, string[] userIds, ConnectedUsersModel connectedUsers)
    {
        List<MapEntryModel> results = await shareStatus
            .OwnedByOrExplicitlySharedWithUser(dataContext, query, userIds, connectedUsers)
            .Select(e => new MapEntryModel()
            {
                Id = e.Id,
                Title = e.Title,
                Location = e.Locations.Select(l => new LocationModel()
                {
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Altitude = l.Altitude
                }).FirstOrDefault()
            })
            .ToListAsync();

        List<MapEntryModel> toRemove = new List<MapEntryModel>();
        foreach (var item in results)
        {
            if (HasNotLocationValue(item))
            {
                var location = await dataContext.Images
                    .Where(i => i.Entry.Id == item.Id && i.Location.Latitude != null && i.Location.Longitude != null)
                    .Select(i => new LocationModel()
                    {
                        Latitude = i.Location.Latitude,
                        Longitude = i.Location.Longitude,
                        Altitude = i.Location.Altitude
                    })
                    .FirstOrDefaultAsync();

                item.Location = location;
            }

            if (HasNotLocationValue(item))
            {
                var location = await dataContext.Videos
                    .Where(v => v.Entry.Id == item.Id && v.Location.Latitude != null && v.Location.Longitude != null)
                    .Select(v => new LocationModel()
                    {
                        Latitude = v.Location.Latitude,
                        Longitude = v.Location.Longitude,
                        Altitude = v.Location.Altitude
                    })
                    .FirstOrDefaultAsync();

                item.Location = location;
            }

            if (HasNotLocationValue(item))
                toRemove.Add(item);
        }

        foreach (var item in toRemove)
            results.Remove(item);

        return results;
    }

    private bool HasNotLocationValue(MapEntryModel item) => item.Location == null || !item.Location.HasValue();
}