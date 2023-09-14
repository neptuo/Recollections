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

        protected async Task<IActionResult> RunEntryAsync(string entryId, Permission sharePermission, Func<Entry, Task<IActionResult>> handler)
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
            Permission? actualPermission = await shareStatus.GetEntryPermissionAsync(entity, userId);
            if (actualPermission == null)
                return Unauthorized();

            if (sharePermission == Permission.CoOwner && actualPermission.Value == Permission.Read)
                return Unauthorized();

            return await handler(entity);
        }

        protected Task<IActionResult> RunStoryAsync(string storyId, Func<Story, Task<IActionResult>> handler)
            => RunStoryAsync(storyId, null, handler);

        protected async Task<IActionResult> RunStoryAsync(string storyId, Permission? sharePermission, Func<Story, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(storyId, "storyId");

            Story entity;
            if (runStoryObserver == null)
                entity = await db.Stories.FindAsync(storyId);
            else
                entity = await runStoryObserver(db.Stories).FirstOrDefaultAsync(s => s.Id == storyId);

            if (entity == null)
                return NotFound();

            string userId = HttpContext.User.FindUserId();
            if (entity.UserId != userId)
            {
                if (sharePermission == null)
                    return Unauthorized();
                else if (sharePermission == Permission.Read && !await shareStatus.IsStorySharedForReadAsync(storyId, userId))
                    return Unauthorized();
                else if (sharePermission == Permission.CoOwner && !await shareStatus.IsStorySharedForWriteAsync(storyId, userId))
                    return Unauthorized();
            }

            return await handler(entity);
        }

        protected Task<IActionResult> RunBeingAsync(string beingId, Func<Being, Task<IActionResult>> handler)
            => RunBeingAsync(beingId, null, handler);

        protected async Task<IActionResult> RunBeingAsync(string beingId, Permission? sharePermission, Func<Being, Task<IActionResult>> handler)
        {
            Ensure.NotNullOrEmpty(beingId, "beingId");

            Being entity = await db.Beings.FindAsync(beingId);
            if (entity == null)
                return NotFound();

            string userId = HttpContext.User.FindUserId();
            if (entity.UserId != userId)
            {
                if (sharePermission == null)
                    return Unauthorized();
                else if (sharePermission == Permission.Read && !await shareStatus.IsBeingSharedForReadAsync(beingId, userId))
                    return Unauthorized();
                else if (sharePermission == Permission.CoOwner && !await shareStatus.IsBeingSharedForWriteAsync(beingId, userId))
                    return Unauthorized();
            }

            return await handler(entity);
        }
    }
}
