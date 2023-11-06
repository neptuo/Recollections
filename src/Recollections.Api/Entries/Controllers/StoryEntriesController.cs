using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.File.Protocol;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;
        private readonly TimelineService timeline;
        private readonly IConnectionProvider connections;

        public StoryEntriesController(DataContext db, ShareStatusService shareStatus, TimelineService timeline, IConnectionProvider connections)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(timeline, "timeline");
            Ensure.NotNull(connections, "connections");
            this.db = db;
            this.shareStatus = shareStatus;
            this.timeline = timeline;
            this.connections = connections;
        }

        [HttpGet("timeline")]
        [ProducesDefaultResponseType(typeof(TimelineListResponse))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> GetStoryTimeline(string storyId) => RunStoryAsync(storyId, Permission.Read, async story =>
        {
            var userId = User.FindUserId();

            var connectionReadUserIds = await connections.GetUserIdsWithReaderToAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries.Where(e => e.Story.Id == storyId).OrderBy(e => e.When), userId, connectionReadUserIds);

            var (models, hasMore) = await timeline.GetAsync(query, userId, Enumerable.Empty<string>(), null);
            return Ok(new TimelineListResponse(models, hasMore));
        });

        [HttpGet("chapters/{chapterId}/timeline")]
        [ProducesDefaultResponseType(typeof(TimelineListResponse))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> GetChapterTimeline(string storyId, string chapterId) => RunStoryAsync(storyId, Permission.Read, async story =>
        {
            var userId = User.FindUserId();

            var connectionReadUserIds = await connections.GetUserIdsWithReaderToAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries.Where(e => e.Chapter.Id == chapterId).OrderBy(e => e.When), userId, connectionReadUserIds);

            var (models, hasMore) = await timeline.GetAsync(query, userId, Enumerable.Empty<string>(), null);
            return Ok(new TimelineListResponse(models, hasMore));
        });
    }
}
