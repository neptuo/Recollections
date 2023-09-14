using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Sharing;
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
    [Route("api/entries/{entryId}/story")]
    public class EntryStoryController : ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;

        public EntryStoryController(DataContext db, ShareStatusService shareStatus)
            : base(db, shareStatus, RunEntryModifier)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.db = db;
            this.shareStatus = shareStatus;
        }

        private static IQueryable<Entry> RunEntryModifier(IQueryable<Entry> query)
        {
            return query
                .Include(e => e.Chapter)
                .Include(e => e.Chapter.Story)
                .Include(e => e.Story);
        }

        [HttpGet]
        [ProducesDefaultResponseType(typeof(EntryStoryModel))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> Get(string entryId) => RunEntryAsync(entryId, Permission.Read, entry =>
        {
            StoryChapter chapter = null;
            Story story = null;
            if (entry.Chapter != null)
            {
                chapter = entry.Chapter;
                story = entry.Chapter.Story;
            }
            else if (entry.Story != null)
            {
                story = entry.Story;
            }

            var model = new EntryStoryModel()
            {
                StoryId = story?.Id,
                StoryTitle = story?.Title,
                ChapterId = chapter?.Id,
                ChapterTitle = chapter?.Title
            };

            return Task.FromResult<IActionResult>(Ok(model));
        });

        [HttpPut]
        public Task<IActionResult> Update(string entryId, EntryStoryUpdateModel model) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            string userId = HttpContext.User.FindUserId();
            Story story = null;
            StoryChapter chapter = null;

            if (model.StoryId != null)
            {
                story = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Stories, userId)
                    .Where(s => s.Id == model.StoryId)
                    .Include(s => s.Chapters)
                    .FirstOrDefaultAsync();

                if (story == null)
                {
                    model.StoryId = null;
                    model.ChapterId = null;
                }
                else if (story.UserId != entry.UserId)
                {
                    return BadRequest();
                }
            }

            if (model.ChapterId != null)
            {
                chapter = story.Chapters.FirstOrDefault(c => c.Id == model.ChapterId);
                if (chapter == null)
                    story = null;
            }

            if (story == null)
            {
                entry.Story = null;
                entry.Chapter = null;
            }
            else if (chapter == null)
            {
                entry.Story = story;
                entry.Chapter = null;
            }
            else
            {
                entry.Story = null;
                entry.Chapter = chapter;
            }

            db.Entries.Update(entry);
            await db.SaveChangesAsync();

            return NoContent();
        });
    }
}
