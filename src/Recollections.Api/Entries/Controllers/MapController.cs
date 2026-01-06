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
    public class MapController(DataContext dataContext, ShareStatusService shareStatus, IConnectionProvider connections, IHttpClientFactory httpFactory) : Controller
    {
        private readonly HttpClient http = httpFactory.CreateClient("mapy.cz");

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

            return Ok(results);
        }

        private bool HasNotLocationValue(MapEntryModel item) => item.Location == null || !item.Location.HasValue();

        [HttpGet]
        public async Task<IActionResult> GeoLocate([FromQuery(Name = "q")] string query)
        {
            var response = await http.GetAsync($"/v1/geocode?query={UrlEncoder.Default.Encode(query)}&limit=10&type=regional&type=poi");
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