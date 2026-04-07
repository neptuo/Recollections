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
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;

namespace Neptuo.Recollections.Entries.Controllers
{
    [Authorize]
    public class TimelineController : ControllerBase
    {
        private const string LastVisitPropertyKey = "Timeline.LastVisit";

        private readonly DataContext db;
        private readonly AccountsDataContext accountsDb;
        private readonly ShareStatusService shareStatus;
        private readonly EntryListMapper entryMapper;
        private readonly IConnectionProvider connections;

        public TimelineController(DataContext db, AccountsDataContext accountsDb, ShareStatusService shareStatus, EntryListMapper entryMapper, IConnectionProvider connections)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(accountsDb, "accountsDb");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(entryMapper, "entryMapper");
            Ensure.NotNull(connections, "connectionProvider");
            this.db = db;
            this.accountsDb = accountsDb;
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

        [HttpPost("api/timeline/visit")]
        public async Task<IActionResult> Visit()
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var property = await accountsDb.UserProperties
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Key == LastVisitPropertyKey);

            int newEntriesCount = 0;
            if (property != null && DateTime.TryParse(property.Value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var lastVisitAt))
            {
                var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
                newEntriesCount = await shareStatus.OwnedByOrExplicitlySharedWithUser(
                    db,
                    db.Entries.Where(e => e.Created > lastVisitAt),
                    userId,
                    connectedUsers
                )
                .Where(e => e.UserId != userId)
                .CountAsync();
            }

            var now = DateTime.Now;
            if (property == null)
            {
                property = new UserPropertyValue
                {
                    UserId = userId,
                    Key = LastVisitPropertyKey,
                    Value = now.ToString("O")
                };
                accountsDb.UserProperties.Add(property);
            }
            else
            {
                property.Value = now.ToString("O");
            }

            await accountsDb.SaveChangesAsync();

            return Ok(new TimelineVisitResponse(newEntriesCount));
        }
    }
}
