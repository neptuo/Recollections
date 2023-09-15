using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries.Stories;
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
    [Route("api/stories/{storyId}")]
    public class StoryEntriesController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;
        private readonly IConnectionProvider connections;

        public StoryEntriesController(DataContext db, ShareStatusService shareStatus, IConnectionProvider connections)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(connections, "connections");
            this.db = db;
            this.shareStatus = shareStatus;
            this.connections = connections;
        }

        [HttpGet("entries")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public async Task<ActionResult<List<StoryEntryModel>>> GetStoryEntryList(string storyId)
        {
            var userId = User.FindUserId();
            var connectionReadUserIds = await connections.GetUserIdsWithReaderToAsync(userId);

            var models = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId, connectionReadUserIds)
                .Where(e => e.Story.Id == storyId)
                .OrderBy(e => e.When)
                .Select(e => new StoryEntryModel()
                {
                    Id = e.Id,
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
            var userId = User.FindUserId();
            var connectionReadUserIds = await connections.GetUserIdsWithReaderToAsync(userId);

            var models = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId, connectionReadUserIds)
                .Where(e => e.Chapter.Story.Id == storyId && e.Chapter.Id == chapterId)
                .OrderBy(e => e.When)
                .Select(e => new StoryEntryModel()
                {
                    Id = e.Id,
                    Title = e.Title
                })
                .ToListAsync();

            return Ok(models);
        }
    }
}
