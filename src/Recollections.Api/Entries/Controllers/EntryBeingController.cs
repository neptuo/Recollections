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
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Neptuo.Recollections.Entries.Controllers
{
    [ApiController]
    [Route("api/entries/{entryId}/beings")]
    public class EntryBeingController : ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;

        public EntryBeingController(DataContext db, ShareStatusService shareStatus)
            : base(db, shareStatus, RunEntryObserver)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.db = db;
            this.shareStatus = shareStatus;
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
        public Task<IActionResult> Get(string entryId) => RunEntryAsync(entryId, Permission.Read, entry =>
        {
            var models = entry.Beings
                .OrderBy(b => b.Name)
                .Select(b => new EntryBeingModel()
                {
                    Id = b.Id,
                    Name = b.Name
                });

            return Task.FromResult<IActionResult>(Ok(models));
        });

        [HttpPut]
        public Task<IActionResult> Update(string entryId, List<string> beingIds) => RunEntryAsync(entryId, Permission.CoOwner, async entry =>
        {
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

            var toAdd = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Beings, userId)
                .Where(b => beingIds.Contains(b.Id))
                .ToListAsync();

            foreach (var being in toAdd)
                entry.Beings.Add(being);

            await db.SaveChangesAsync();

            return NoContent();
        });
    }
}
