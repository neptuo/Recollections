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
        private readonly DataContext dataContext;
        private readonly IUserNameProvider userNames;
        private readonly ShareStatusService shareStatus;
        private readonly TimelineService timeline;

        public TimelineController(DataContext dataContext, IUserNameProvider userNames, ShareStatusService shareStatus, TimelineService timeline)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(timeline, "timeline");
            this.dataContext = dataContext;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
            this.timeline = timeline;
        }

        [HttpGet("api/timeline/list")]
        public async Task<IActionResult> List(int offset)
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var query = HttpContext.User.FindUserId() == userId
                ? shareStatus.OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId)
                : dataContext.Entries.Where(e => e.UserId == userId);

            var (models, hasMore) = await timeline.GetAsync(query, userId, offset);
            return Ok(new TimelineListResponse(models, hasMore));
        }
    }
}
