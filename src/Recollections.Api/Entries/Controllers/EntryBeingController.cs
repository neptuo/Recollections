using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Accounts.Notifications;
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
    [Route("api/entries/{entryId}/beings")]
    public class EntryBeingController : ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;
        private readonly IConnectionProvider connections;
        private readonly NewEntriesNotificationNotifier notificationNotifier;

        public EntryBeingController(DataContext db, ShareStatusService shareStatus, IConnectionProvider connections, NewEntriesNotificationNotifier notificationNotifier)
            : base(db, shareStatus, RunEntryObserver)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(connections, "connections");
            Ensure.NotNull(notificationNotifier, "notificationNotifier");
            this.db = db;
            this.shareStatus = shareStatus;
            this.connections = connections;
            this.notificationNotifier = notificationNotifier;
        }

        private static IQueryable<Entry> RunEntryObserver(IQueryable<Entry> query)
        {
            return query.Include(e => e.Beings);
        }

        [HttpGet]
        [ProducesDefaultResponseType(typeof(List<EntryBeingModel>))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> Get(string entryId) => RunEntryAsync(entryId, Permission.Read, async entry =>
        {
            string userId = User.FindUserId();
            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
            var beingIds = entry.Beings.Select(b => b.Id).ToList();

            var models = await shareStatus
                .OwnedByOrExplicitlySharedWithUser(db, db.Beings.Where(b => beingIds.Contains(b.Id)), userId, connectedUsers)
                .OrderBy(b => b.Name)
                .Select(b => new EntryBeingModel
                (
                    Id: b.Id,
                    Name: b.Name,
                    Icon: b.Icon
                ))
                .ToListAsync();

            return Ok(models);
        });

        [HttpPut]
        public Task<IActionResult> Update(string entryId, List<string> beingIds) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
            NewEntriesNotificationSnapshot beforeSnapshot = await notificationNotifier.CaptureEntriesAsync(entry.Id);
            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            var toRemove = new List<Being>();
            foreach (var being in entry.Beings)
            {
                if (!beingIds.Contains(being.Id))
                    toRemove.Add(being);
                else
                    beingIds.Remove(being.Id);
            }

            foreach (var being in toRemove)
                entry.Beings.Remove(being);

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            var toAdd = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Beings, userId, connectedUsers)
                .Where(b => beingIds.Contains(b.Id))
                .ToListAsync();

            foreach (var being in toAdd)
                entry.Beings.Add(being);

            await db.SaveChangesAsync();
            await notificationNotifier.NotifyEntriesAsync(new[] { entry.Id }, beforeSnapshot, "entry-beings-update");

            return NoContent();
        });
    }
}
