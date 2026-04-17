using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers;

[Authorize]
[Route("api/highest-altitude")]
public class HighestAltitudeController(DataContext dataContext, HighestAltitudeMapper mapper, ShareStatusService shareStatus, IConnectionProvider connections)
    : ControllerBase(dataContext, shareStatus)
{
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
        var accessibleEntries = shareStatus.OwnedByOrExplicitlySharedWithUser(
            dataContext, dataContext.Entries.AsNoTracking(), userId, connectedUsers);

        var models = await mapper.MapAsync(accessibleEntries, userId, [userId], connectedUsers);
        return Ok(models);
    }
}
