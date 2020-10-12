using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
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
    [Route("api/timeline/[action]")]
    public class TimelineController : ControllerBase
    {
        private const int PageSize = 10;

        private readonly DataContext dataContext;
        private readonly IUserNameProvider userNames;

        public TimelineController(DataContext dataContext, IUserNameProvider userNames)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(userNames, "userNames");
            this.dataContext = dataContext;
            this.userNames = userNames;
        }

        [HttpGet]
        public async Task<IActionResult> List(int offset)
        {
            Ensure.PositiveOrZero(offset, "offset");

            string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            List<TimelineEntryModel> result = await dataContext.Entries
                .Where(e => e.UserId == userId || dataContext.EntryShares.Any(s => s.EntryId == e.Id && s.UserId == userId))
                .OrderByDescending(e => e.When)
                .Skip(offset)
                .Take(PageSize)
                .Select(e => new TimelineEntryModel()
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    Title = e.Title,
                    When = e.When,
                    StoryTitle = e.Story.Title ?? e.Chapter.Story.Title,
                    ChapterTitle = e.Chapter.Title,
                    GpsCount = e.Locations.Count,
                    ImageCount = dataContext.Images.Count(i => i.Entry.Id == e.Id)
                })
                .ToListAsync();

            var userNames = await this.userNames.GetUserNamesAsync(result.Select(e => e.UserId).ToArray());
            for (int i = 0; i < result.Count; i++)
                result[i].UserName = userNames[i];

            return Ok(new TimelineListResponse(result, result.Count == PageSize));
        }
    }
}
