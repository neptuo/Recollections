using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
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
    public class TimelineController : ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;
        private readonly TimelineService timeline;
        private readonly IConnectionProvider connections;

        public TimelineController(DataContext db, ShareStatusService shareStatus, TimelineService timeline, IConnectionProvider connections)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(timeline, "timeline");
            Ensure.NotNull(connections, "connectionProvider");
            this.db = db;
            this.shareStatus = shareStatus;
            this.timeline = timeline;
            this.connections = connections;
        }

        [HttpGet("api/timeline/list")]
        public async Task<IActionResult> List(int offset)
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectionReadUserIds = await connections.GetUserIdsWithReaderToAsync(userId);

            var query = HttpContext.User.FindUserId() == userId
                ? shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Entries, userId, connectionReadUserIds)
                : db.Entries.Where(e => e.UserId == userId);

            var (models, hasMore) = await timeline.GetAsync(query, userId, connectionReadUserIds, offset);
            return Ok(new TimelineListResponse(models, hasMore));
        }
    }
}
