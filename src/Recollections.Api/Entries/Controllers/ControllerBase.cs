using Microsoft.AspNetCore.Http;
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
    public class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;
        private readonly Func<IQueryable<Entry>, IQueryable<Entry>> runEntryObserver;
        private readonly Func<IQueryable<Story>, IQueryable<Story>> runStoryObserver;

        protected ControllerBase(DataContext db, ShareStatusService shareStatus, Func<IQueryable<Entry>, IQueryable<Entry>> runEntryObserver = null, Func<IQueryable<Story>, IQueryable<Story>> runStoryObserver = null)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.db = db;
            this.shareStatus = shareStatus;
            this.runEntryObserver = runEntryObserver;
            this.runStoryObserver = runStoryObserver;
        }

        protected IActionResult PremiumRequired()
            => StatusCode(StatusCodes.Status402PaymentRequired);

        private async Task<IActionResult> RunAsync<T>(T entity, Permission sharePermission, Func<T, string, Task<Permission?>> permissionGetter, Func<T, Permission, Task<IActionResult>> handler)
        {            
            if (entity == null)
                return NotFound();

            string userId = HttpContext.User.FindUserId();
            if (userId == null)
                return Unauthorized();

            Permission? actualPermission = await permissionGetter(entity, userId);
            if (actualPermission == null)
                return Unauthorized();

            if (sharePermission == Permission.CoOwner && actualPermission.Value == Permission.Read)
                return Unauthorized();

            return await handler(entity, actualPermission.Value);
        }

        protected Task<IActionResult> RunEntryAsync(string entryId, Permission sharePermission, Func<Entry, Task<IActionResult>> handler)
            => RunEntryAsync(entryId, sharePermission, (entity, permission) => handler(entity));

        protected async Task<IActionResult> RunEntryAsync(string entryId, Permission sharePermission, Func<Entry, Permission, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(entryId, "entryId");

            Entry entity;
            if (runEntryObserver == null)
                entity = await db.Entries.FindAsync(entryId);
            else
                entity = await runEntryObserver(db.Entries).FirstOrDefaultAsync(e => e.Id == entryId);

            return await RunAsync(entity, sharePermission, shareStatus.GetEntryPermissionAsync, handler);
        }

        protected Task<IActionResult> RunStoryAsync(string entryId, Permission sharePermission, Func<Story, Task<IActionResult>> handler)
            => RunStoryAsync(entryId, sharePermission, (entity, permission) => handler(entity));

        protected async Task<IActionResult> RunStoryAsync(string storyId, Permission sharePermission, Func<Story, Permission, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(storyId, "storyId");

            Story entity;
            if (runStoryObserver == null)
                entity = await db.Stories.FindAsync(storyId);
            else
                entity = await runStoryObserver(db.Stories).FirstOrDefaultAsync(s => s.Id == storyId);
                
            return await RunAsync(entity, sharePermission, shareStatus.GetStoryPermissionAsync, handler);
        }

        protected Task<IActionResult> RunBeingAsync(string entryId, Permission sharePermission, Func<Being, Task<IActionResult>> handler)
            => RunBeingAsync(entryId, sharePermission, (entity, permission) => handler(entity));

        protected async Task<IActionResult> RunBeingAsync(string beingId, Permission sharePermission, Func<Being, Permission, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(beingId, "beingId");

            Being entity = await db.Beings.FindAsync(beingId);
            return await RunAsync(entity, sharePermission, shareStatus.GetBeingPermissionAsync, handler);
        }
    }
}
