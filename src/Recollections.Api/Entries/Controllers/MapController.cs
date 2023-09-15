using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [Authorize]
    [Route("api/map/[action]")]
    public class MapController : Controller
    {
        private readonly DataContext dataContext;
        private readonly ShareStatusService shareStatus;
        private readonly IConnectionProvider connections;

        public MapController(DataContext dataContext, ShareStatusService shareStatus, IConnectionProvider connections)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(connections, "connections");
            this.dataContext = dataContext;
            this.shareStatus = shareStatus;
            this.connections = connections;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectionReadUserIds = await connections.GetUserIdsWithReaderToAsync(userId);

            List<MapEntryModel> results = await shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectionReadUserIds)
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
                if (HasLocationValue(item))
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

                if (HasLocationValue(item))
                    toRemove.Add(item);
            }

            foreach (var item in toRemove)
                results.Remove(item);

            return Ok(results);
        }

        private bool HasLocationValue(MapEntryModel item) => item.Location == null || !item.Location.HasValue();
    }
}