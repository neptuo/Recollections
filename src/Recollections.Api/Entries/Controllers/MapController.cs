using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
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
        private readonly MapOptions options;

        public MapController(DataContext dataContext, ShareStatusService shareStatus, IConnectionProvider connections, IOptions<MapOptions> options)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(connections, "connections");
            Ensure.NotNull(options, "options");
            this.dataContext = dataContext;
            this.shareStatus = shareStatus;
            this.connections = connections;
            this.options = options.Value;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            List<MapEntryModel> results = await shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectedUsers)
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

        [HttpGet]
        public async Task<IActionResult> GeoLocate([FromQuery(Name = "q")] string query)
        {
            var response = await http.GetAsync($"https://api.mapy.cz/v1/geocode?apikey={options.ApiKey}&query={UrlEncoder.Default.Encode(query)}&limit=10&type=regional&type=poi");
            response.EnsureSuccessStatusCode();

            var items = await response.Content.ReadFromJsonAsync<GeoLocateRoot>();
            var result = new List<MapSearchModel>();
            foreach (var item in items.Items)
            {
                result.Add(new()
                {
                    Label = $"{item.Name} ({item.Label}), {item.Location}",
                    Latitude = item.Position.Latitude,
                    Longitude = item.Position.Longitude
                });
            }

            return Ok(result);
        }

        static readonly HttpClient http = new();

        public class GeoLocateRoot
        {
            public GeoLocateItem[] Items { get; set; }
        }

        public class GeoLocateItem
        {
            public string Name { get; set; }
            public string Label { get; set; }
            public GeoLocatePosition Position { get; set; }
            public string Location { get; set; }
        }

        public class GeoLocatePosition
        {
            [JsonPropertyName("lon")]
            public float Longitude { get; set; }
            [JsonPropertyName("lat")]
            public float Latitude { get; set; }
        }
    }
}