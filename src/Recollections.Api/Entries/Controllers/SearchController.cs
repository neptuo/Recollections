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

        public SearchController(DataContext dataContext, IUserNameProvider userNames, ShareStatusService shareStatus)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.dataContext = dataContext;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
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

            List<SearchEntryModel> result = await shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId)
                .OrderByDescending(e => e.When)
                .Where(e => e.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || e.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
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
