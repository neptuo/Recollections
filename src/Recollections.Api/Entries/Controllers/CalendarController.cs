using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
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
        private readonly EntryListMapper entryMapper;
        private readonly ShareStatusService shareStatus;
        private readonly IUserPremiumProvider premiumProvider;
        private readonly IConnectionProvider connections;

        public CalendarController(DataContext dataContext, EntryListMapper entryMapper, ShareStatusService shareStatus, IUserPremiumProvider premiumProvider, IConnectionProvider connections)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(entryMapper, "entryMapper");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(premiumProvider, "premiumProvider");
            Ensure.NotNull(connections, "connections");
            this.dataContext = dataContext;
            this.entryMapper = entryMapper;
            this.shareStatus = shareStatus;
            this.premiumProvider = premiumProvider;
            this.connections = connections;
        }

        [HttpGet("{year}")]
        [ProducesDefaultResponseType(typeof(List<EntryListModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetYearList(int year)
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!await premiumProvider.HasPremiumAsync(userId))
                return PremiumRequired();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            var query = shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectedUsers)
                .Where(e => e.When.Year == year)
                .OrderByDescending(e => e.When);

            var (models, _) = await entryMapper.MapAsync(query, userId, connectedUsers);
            return Ok(models);
        }

        [HttpGet("{year}/{month}")]
        [ProducesDefaultResponseType(typeof(List<EntryListModel>))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<EntryListModel>>> GetMonthList(int year, int month)
        {
            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            var query = shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectedUsers)
                .Where(e => e.When.Year == year && e.When.Month == month)
                .OrderByDescending(e => e.When);
                
            var (models, _) = await entryMapper.MapAsync(query, userId, connectedUsers);
            return Ok(models);
        }
    }
}
