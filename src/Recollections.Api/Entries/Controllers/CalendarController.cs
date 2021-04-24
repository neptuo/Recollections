using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers
{
    [Authorize]
    [Route("api/calendar")]
    public class CalendarController : ControllerBase
    {
        private readonly DataContext dataContext;
        private readonly ShareStatusService shareStatus;

        public CalendarController(DataContext dataContext, ShareStatusService shareStatus)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(shareStatus, "shareStatus");
            this.dataContext = dataContext;
            this.shareStatus = shareStatus;
        }

        [HttpGet("{year}/{month}")]
        [ProducesDefaultResponseType(typeof(List<CalendarEntryModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CalendarEntryModel>>> GetMonthList(int year, int month)
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId)
                .Where(e => e.When.Year == year && e.When.Month == month)
                .OrderByDescending(e => e.When)
                .Select(e => new CalendarEntryModel()
                {
                    Id = e.Id,
                    Title = e.Title,
                    When = e.When
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }
    }
}
