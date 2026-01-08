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
    public class MapController(DataContext db, MapService mapService, IConnectionProvider connections, IHttpClientFactory httpFactory) : Controller
    {
        private readonly HttpClient http = httpFactory.CreateClient("mapy.cz");

        [HttpGet]
        public async Task<IActionResult> List()
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var results = await mapService.GetAsync(db.Entries, userId, connectedUsers);
            return Ok(results);
        }

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