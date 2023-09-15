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
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private const int PageSize = 10;

        private readonly DataContext dataContext;
        private readonly IUserNameProvider userNames;
        private readonly ShareStatusService shareStatus;
        private readonly IConnectionProvider connections;

        public SearchController(DataContext dataContext, IUserNameProvider userNames, ShareStatusService shareStatus, IConnectionProvider connections)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(connections, "connections");
            this.dataContext = dataContext;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
            this.connections = connections;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery(Name = "q")] string query, int offset)
        {
            Ensure.PositiveOrZero(offset, "offset");

            if (String.IsNullOrEmpty(query) || String.IsNullOrWhiteSpace(query))
                return BadRequest();

            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectionReadUserIds = await connections.GetUserIdsWithReaderToAsync(userId);

            List<SearchEntryModel> result = await shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectionReadUserIds)
                .OrderByDescending(e => e.When)
                .Where(e => EF.Functions.Like(e.Title, $"%{query}%") || EF.Functions.Like(e.Text, $"%{query}%") || EF.Functions.Like(e.Story.Title, $"%{query}%") || EF.Functions.Like(e.Chapter.Story.Title, $"%{query}%") || EF.Functions.Like(e.Chapter.Title, $"%{query}%"))
                .Skip(offset)
                .Take(PageSize)
                .Select(e => new SearchEntryModel()
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    Title = e.Title,
                    When = e.When,
                    Text = e.Text,
                    StoryTitle = e.Story.Title ?? e.Chapter.Story.Title,
                    ChapterTitle = e.Chapter.Title,
                    GpsCount = e.Locations.Count,
                    ImageCount = dataContext.Images.Count(i => i.Entry.Id == e.Id)
                })
                .ToListAsync();

            var userNames = await this.userNames.GetUserNamesAsync(result.Select(e => e.UserId).ToArray());
            for (int i = 0; i < result.Count; i++)
                result[i].UserName = userNames[i];

            return Ok(new SearchResponse(result, result.Count == PageSize));
        }
    }
}
