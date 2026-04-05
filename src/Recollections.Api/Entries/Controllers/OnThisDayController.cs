using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers;

[Authorize]
[Route("api/on-this-day")]
public class OnThisDayController(DataContext dataContext, EntryListMapper entryMapper, ShareStatusService shareStatus, IConnectionProvider connections)
    : ControllerBase(dataContext, shareStatus)
{
    private IQueryable<Entry> GetOnThisDayQuery(string userId, ConnectedUsersModel connectedUsers)
    {
        var today = DateTime.Today;
        return shareStatus
            .OwnedByOrExplicitlySharedWithUser(dataContext, dataContext.Entries, userId, connectedUsers)
            .Where(e => e.When.Month == today.Month && e.When.Day == today.Day && e.When.Year != today.Year)
            .OrderByDescending(e => e.When);
    }

    [HttpGet]
    [ProducesDefaultResponseType(typeof(List<EntryListModel>))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList()
    {
        string userId = HttpContext.User.FindUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        var query = GetOnThisDayQuery(userId, connectedUsers);

        var (models, _) = await entryMapper.MapAsync(query, userId, connectedUsers);
        return Ok(models);
    }

    [HttpGet("count")]
    [ProducesDefaultResponseType(typeof(int))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCount()
    {
        string userId = HttpContext.User.FindUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        var query = GetOnThisDayQuery(userId, connectedUsers);

        int count = query.Count();
        return Ok(count);
    }
}
