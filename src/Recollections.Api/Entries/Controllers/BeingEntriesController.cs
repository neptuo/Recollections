using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries.Beings;
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
    [Route("api/beings/{beingId}")]
    public class BeingEntriesController : ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;
        private readonly EntryListMapper entryMapper;
        private readonly StoryListMapper storyMapper;
        private readonly IConnectionProvider connections;

        public BeingEntriesController(DataContext db, ShareStatusService shareStatus, EntryListMapper entryMapper, StoryListMapper storyMapper, IConnectionProvider connections)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(entryMapper, "entryMapper");
            Ensure.NotNull(storyMapper, "storyMapper");
            Ensure.NotNull(connections, "connections");
            this.db = db;
            this.shareStatus = shareStatus;
            this.entryMapper = entryMapper;
            this.storyMapper = storyMapper;
            this.connections = connections;
        }

        [HttpGet("timeline")]
        [ProducesDefaultResponseType(typeof(PageableList<EntryListModel>))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> List(string beingId, int offset, int? count = null) => RunBeingAsync(beingId, Permission.Read, async being =>
        {
            var userId = User.FindUserId();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(
                db, 
                db.Entries
                    .Where(e => e.Beings.Any(b => b.Id == beingId))
                    .OrderByDescending(e => e.When), 
                [userId, ShareStatusService.PublicUserId], 
                connectedUsers
            );

            int pageSize = EntryListMapper.NormalizePageSize(count);
            var (models, hasMore) = await entryMapper.MapAsync(query, userId, connectedUsers, offset, pageSize, includePreviewMedia: true);
            return Ok(new PageableList<EntryListModel>(models, hasMore));
        });

        [HttpGet("stories")]
        [ProducesDefaultResponseType(typeof(List<StoryListModel>))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> GetStories(string beingId) => RunBeingAsync(beingId, Permission.Read, async being =>
        {
            var userId = User.FindUserId();
            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            var stories = await shareStatus.OwnedByOrExplicitlySharedWithUser(
                    db,
                    db.Entries
                        .Where(e => e.Beings.Any(b => b.Id == beingId))
                        .Where(e => e.Story != null || e.Chapter != null),
                    [userId, ShareStatusService.PublicUserId],
                    connectedUsers
                )
                .Select(e => e.Story ?? e.Chapter.Story)
                .Distinct()
                .ToListAsync();

            var models = await storyMapper.MapAsync(stories, userId, connectedUsers);

            return Ok(models);
        });
    }
}
