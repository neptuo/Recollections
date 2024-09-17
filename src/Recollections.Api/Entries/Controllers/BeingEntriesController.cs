using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.File.Protocol;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries.Beings;
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
        private readonly TimelineService timeline;
        private readonly IConnectionProvider connections;

        public BeingEntriesController(DataContext db, ShareStatusService shareStatus, TimelineService timeline, IConnectionProvider connections)
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
        public Task<IActionResult> List(string beingId, int offset) => RunBeingAsync(beingId, Permission.Read, async being =>
        {
            var userId = User.FindUserId();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries.Where(e => e.Beings.Any(b => b.Id == beingId)).OrderByDescending(e => e.When), userId, connectedUsers);

            var (models, hasMore) = await timeline.GetAsync(query, userId, connectedUsers, offset);
            return Ok(new TimelineListResponse(models, hasMore));
        });
    }
}
