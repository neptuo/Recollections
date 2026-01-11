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
        private readonly EntryListMapper entryMapper;
        private readonly IConnectionProvider connections;

        public TimelineController(DataContext db, ShareStatusService shareStatus, EntryListMapper entryMapper, IConnectionProvider connections)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(entryMapper, "entryMapper");
            Ensure.NotNull(connections, "connectionProvider");
            this.db = db;
            this.shareStatus = shareStatus;
            this.entryMapper = entryMapper;
            this.connections = connections;
        }

        [HttpGet("api/timeline/list")]
        public async Task<IActionResult> List(int offset)
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var query = shareStatus.OwnedByOrExplicitlySharedWithUser(
                db, 
                db.Entries.OrderByDescending(e => e.When), 
                userId, 
                connectedUsers
            );

            var (models, hasMore) = await entryMapper.MapAsync(query, userId, connectedUsers, offset);
            return Ok(new PageableList<EntryListModel>(models, hasMore));
        }
    }
}
