using Microsoft.AspNetCore.Mvc;
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
        private readonly EntryListMapper entryMapper;
        private readonly IConnectionProvider connections;

        public StoryEntriesController(DataContext db, ShareStatusService shareStatus, EntryListMapper entryMapper, IConnectionProvider connections)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(entryMapper, "entryMapper");
            Ensure.NotNull(connections, "connections");
            this.db = db;
            this.shareStatus = shareStatus;
            this.entryMapper = entryMapper;
            this.connections = connections;
        }

        [HttpGet("timeline")]
        [ProducesDefaultResponseType(typeof(PageableList<EntryListModel>))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> GetStoryTimeline(string storyId) => RunStoryAsync(storyId, Permission.Read, async story =>
        {
            var userId = User.FindUserId();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(
                db, 
                db.Entries
                    .Where(e => e.Story.Id == storyId)
                    .OrderBy(e => e.When), 
                [userId, ShareStatusService.PublicUserId], 
                connectedUsers
            );

            var (models, hasMore) = await entryMapper.MapAsync(query, userId, connectedUsers);
            return Ok(new PageableList<EntryListModel>(models, hasMore));
        });

        [HttpGet("chapters/{chapterId}/timeline")]
        [ProducesDefaultResponseType(typeof(PageableList<EntryListModel>))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> GetChapterTimeline(string storyId, string chapterId) => RunStoryAsync(storyId, Permission.Read, async story =>
        {
            var userId = User.FindUserId();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries.Where(e => e.Chapter.Id == chapterId).OrderBy(e => e.When), [userId, ShareStatusService.PublicUserId], connectedUsers);

            var (models, hasMore) = await entryMapper.MapAsync(query, userId, connectedUsers);
            return Ok(new PageableList<EntryListModel>(models, hasMore));
        });
    }
}
