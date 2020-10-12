using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    public class EntryControllerBase : ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;
        private readonly Func<IQueryable<Entry>, IQueryable<Entry>> runEntryObserver;

        protected EntryControllerBase(DataContext db, ShareStatusService shareStatus, Func<IQueryable<Entry>, IQueryable<Entry>> runEntryObserver = null)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.db = db;
            this.shareStatus = shareStatus;
            this.runEntryObserver = runEntryObserver;
        }

        protected Task<IActionResult> RunEntryAsync(string entryId, Func<Entry, Task<IActionResult>> handler)
            => RunEntryAsync(entryId, null, handler);

        protected async Task<IActionResult> RunEntryAsync(string entryId, Permission? sharePermission, Func<Entry, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(entryId, "entryId");

            Entry entity;
            if (runEntryObserver == null)
                entity = await db.Entries.FindAsync(entryId);
            else
                entity = await runEntryObserver(db.Entries).FirstOrDefaultAsync(e => e.Id == entryId);

            if (entity == null)
                return NotFound();

            string userId = HttpContext.User.FindUserId();
            if (entity.UserId != userId)
            {
                if (sharePermission == null)
                    return Unauthorized();
                else if (sharePermission == Permission.Read && !await shareStatus.IsEntrySharedForReadAsync(entryId, userId))
                    return Unauthorized();
                else if (sharePermission == Permission.Write && !await shareStatus.IsEntrySharedForWriteAsync(entryId, userId))
                    return Unauthorized();
            }

            return await handler(entity);
        }
    }
}
