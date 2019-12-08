using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
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

        public TimelineController(DataContext dataContext)
        {
            Ensure.NotNull(dataContext, "dataContext");
            this.dataContext = dataContext;
        }

        [HttpGet]
        public async Task<IActionResult> List(int offset)
        {
            return StatusCode(503);

            Ensure.PositiveOrZero(offset, "offset");

            string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            List<TimelineEntryModel> result = await dataContext.Entries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.When)
                .Skip(offset)
                .Take(PageSize)
                .Select(e => new TimelineEntryModel()
                {
                    Id = e.Id,
                    Title = e.Title,
                    When = e.When,
                    StoryTitle = e.Story.Title ?? e.Chapter.Story.Title,
                    ChapterTitle = e.Chapter.Title,
                    GpsCount = e.Locations.Count
                })
                .ToListAsync();

            foreach (var entry in result)
                entry.ImageCount = await dataContext.Images.CountAsync(i => i.Entry.Id == entry.Id);

            return Ok(new TimelineListResponse(result, result.Count == PageSize));
        }
    }
}
