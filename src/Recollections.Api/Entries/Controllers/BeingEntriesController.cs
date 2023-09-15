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
    public class BeingEntriesController : ControllerBase
    {
        private readonly DataContext db;
        private readonly TimelineService timeline;

        public BeingEntriesController(DataContext db, ShareStatusService shareStatus, TimelineService timeline)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(timeline, "timeline");
            this.db = db;
            this.timeline = timeline;
        }

        [HttpGet("timeline")]
        [ProducesDefaultResponseType(typeof(TimelineListResponse))]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status401Unauthorized)]
        public Task<IActionResult> List(string beingId, int offset) => RunBeingAsync(beingId, Permission.Read, async being =>
        {
            var userId = User.FindUserId();
            
            var query = db.Entries.Where(e => e.Beings.Any(b => b.Id == beingId));

            var (models, hasMore) = await timeline.GetAsync(query, userId, Enumerable.Empty<string>(), offset);
            return Ok(new TimelineListResponse(models, hasMore));
        });
    }
}
