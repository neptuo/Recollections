using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
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
        private readonly IConnectionProvider connections;

        public EntryStoryController(DataContext db, ShareStatusService shareStatus, IConnectionProvider connections)
            : base(db, shareStatus, RunEntryModifier)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(connections, "connections");
            this.db = db;
            this.shareStatus = shareStatus;
            this.connections = connections;
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
            string userId = User.FindUserId();
            Story story = null;
            StoryChapter chapter = null;

            if (model.StoryId != null)
            {
                var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

                story = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Stories, userId, connectedUsers)
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
                    // Owner of the entry needs co-owner permission to story
                    // Owner of the story needs co-owner permission to entry
                    var entryUserStoryPermission = await shareStatus.GetStoryPermissionAsync(story, entry.UserId);
                    var storyUserEntryPermission = await shareStatus.GetEntryPermissionAsync(entry, story.UserId);
                    if (entryUserStoryPermission != Permission.CoOwner || storyUserEntryPermission != Permission.CoOwner)
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
