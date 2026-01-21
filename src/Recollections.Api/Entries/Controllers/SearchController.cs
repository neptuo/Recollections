using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly DataContext dataContext;
        private readonly IUserNameProvider userNames;
        private readonly ShareStatusService shareStatus;
        private readonly IConnectionProvider connections;
        private readonly EntryListMapper entryMapper;

        public SearchController(DataContext dataContext, EntryListMapper entryMapper, IUserNameProvider userNames, ShareStatusService shareStatus, IConnectionProvider connections)
            : base(dataContext, shareStatus)
        {
            Ensure.NotNull(dataContext, "dataContext");
            Ensure.NotNull(entryMapper, "entryMapper");
            Ensure.NotNull(userNames, "userNames");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(connections, "connections");
            this.dataContext = dataContext;
            this.entryMapper = entryMapper;
            this.userNames = userNames;
            this.shareStatus = shareStatus;
            this.connections = connections;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery(Name = "q")] string query, int offset)
        {
            Ensure.PositiveOrZero(offset, "offset");

            if (String.IsNullOrEmpty(query) || String.IsNullOrWhiteSpace(query))
                return BadRequest();

            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            var dbQuery = shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectedUsers)
                .OrderByDescending(e => e.When)
                .Where(e => EF.Functions.Like(e.Title, $"%{query}%") || EF.Functions.Like(e.Text, $"%{query}%") || EF.Functions.Like(e.Story.Title, $"%{query}%") || EF.Functions.Like(e.Chapter.Story.Title, $"%{query}%") || EF.Functions.Like(e.Chapter.Title, $"%{query}%"));
            
            var (models, hasMore) = await entryMapper.MapAsync(dbQuery, userId, connectedUsers, offset);
            return Ok(new PageableList<EntryListModel>(models, hasMore));
        }
    }
}
