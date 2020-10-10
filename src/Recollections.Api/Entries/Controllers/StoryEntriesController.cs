using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Entries.Stories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/stories/{storyId}")]
    public class StoryEntriesController : ControllerBase
    {
        private readonly DataContext dataContext;

        public StoryEntriesController(DataContext dataContext)
        {
            Ensure.NotNull(dataContext, "dataContext");
            this.dataContext = dataContext;
        }

        [HttpGet("entries")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public async Task<ActionResult<List<StoryEntryModel>>> GetStoryEntryList(string storyId)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            var models = await dataContext.Entries
                .Where(e => e.UserId == userId)
                .Where(e => e.Story.Id == storyId)
                .Select(e => new StoryEntryModel()
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    Title = e.Title
                })
                .ToListAsync();

            return Ok(models);
        }

        [HttpGet("chapters/{chapterId}/entries")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public async Task<ActionResult<List<StoryEntryModel>>> GetChapterEntryList(string storyId, string chapterId)
        {
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            var models = await dataContext.Entries
                .Where(e => e.UserId == userId)
                .Where(e => e.Chapter.Story.Id == storyId && e.Chapter.Id == chapterId)
                .Select(e => new StoryEntryModel()
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    Title = e.Title
                })
                .ToListAsync();

            return Ok(models);
        }
    }
}
