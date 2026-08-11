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
    [Route("api/entries/search")]
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
        public async Task<IActionResult> Get([FromQuery(Name = "q")] string query, [FromQuery(Name = "being")] string[] beingIds, [FromQuery(Name = "from")] DateTime? dateFrom, [FromQuery(Name = "to")] DateTime? dateTo, int offset)
        {
            Ensure.PositiveOrZero(offset, "offset");

            List<string> selectedBeingIds = beingIds?
                .Where(id => !String.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList()
                ?? [];

            bool hasQuery = !String.IsNullOrWhiteSpace(query);
            if (!hasQuery && selectedBeingIds.Count == 0 && dateFrom == null && dateTo == null)
                return BadRequest();
            if (dateFrom != null && dateTo != null && dateFrom.Value.Date > dateTo.Value.Date)
                return BadRequest();

            string userId = HttpContext.User.FindUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

            if (selectedBeingIds.Count > 0)
            {
                var accessibleBeingIds = await shareStatus
                    .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Beings.Where(b => selectedBeingIds.Contains(b.Id)), userId, connectedUsers)
                    .Select(b => b.Id)
                    .ToListAsync();

                if (accessibleBeingIds.Count != selectedBeingIds.Count)
                    return Unauthorized();
            }

            var dbQuery = shareStatus
                .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectedUsers)
                .AsQueryable();

            if (hasQuery)
                dbQuery = dbQuery.Where(e => EF.Functions.Like(e.Title, $"%{query}%") || EF.Functions.Like(e.Text, $"%{query}%") || EF.Functions.Like(e.Story.Title, $"%{query}%") || EF.Functions.Like(e.Chapter.Story.Title, $"%{query}%") || EF.Functions.Like(e.Chapter.Title, $"%{query}%"));
            if (selectedBeingIds.Count > 0)
                dbQuery = dbQuery.Where(e => selectedBeingIds.All(beingId => e.Beings.Any(b => b.Id == beingId)));
            if (dateFrom != null)
                dbQuery = dbQuery.Where(e => e.When >= dateFrom.Value.Date);
            if (dateTo != null)
                dbQuery = dbQuery.Where(e => e.When < dateTo.Value.Date.AddDays(1));

            dbQuery = dbQuery.OrderByDescending(e => e.When);
            
            var (models, hasMore) = await entryMapper.MapAsync(dbQuery, [userId], connectedUsers, offset);
            return Ok(new PageableList<EntryListModel>(models, hasMore));
        }
    }
}
