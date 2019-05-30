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

namespace Neptuo.Recollection.Entries.Controllers
{
    [Authorize]
    [Route("api/entries/[action]")]
    public class TimelineController : ControllerBase
    {
        private readonly DataContext dataContext;

        public TimelineController(DataContext dataContext)
        {
            Ensure.NotNull(dataContext, "dataContext");
            this.dataContext = dataContext;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            List<TimelineEntryModel> result = await dataContext.Entries
                .Where(e => e.UserId == userId)
                .Take(10)
                .Select(e => new TimelineEntryModel()
                {
                    Title = e.Title,
                    When = e.When,
                    Text = e.Text
                })
                .ToListAsync();

            return Ok(new TimelineListResponse(result));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EntryCreateRequest request)
        {
            string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await dataContext.Entries.AddAsync(new Entry()
            {
                Title = request.Title,
                When = request.When,
                UserId = userId
            });

            await dataContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            Entry entity = await dataContext.Entries.FindAsync(id);
            if (entity == null)
                return NotFound();

            if (entity.UserId != userId)
                return Unauthorized();

            dataContext.Entries.Remove(entity);
            await dataContext.SaveChangesAsync();

            return Ok();
        }
    }
}
