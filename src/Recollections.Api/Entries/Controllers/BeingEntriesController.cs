using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class BeingEntriesController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private readonly DataContext db;
        private readonly ShareStatusService shareStatus;

        public BeingEntriesController(DataContext db, ShareStatusService shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.db = db;
            this.shareStatus = shareStatus;
        }

        [HttpGet("entries")]
        [ProducesDefaultResponseType(typeof(EntryModel))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public async Task<ActionResult<List<BeingEntryModel>>> GetStoryEntryList(string beingId)
        {
            string userId = HttpContext.User.FindUserId();
            var models = await shareStatus.OwnedByOrExplicitlySharedWithUser(db, db.Beings.Where(b => b.Id == beingId).SelectMany(b => b.Entries), userId)
                .OrderBy(e => e.When)
                .Select(e => new BeingEntryModel()
                {
                    Id = e.Id,
                    Title = e.Title
                })
                .ToListAsync();

            return Ok(models);
        }
    }
}
