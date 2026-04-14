using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers;

[Authorize]
[Route("api/highest-altitude")]
public class HighestAltitudeController(DataContext dataContext, HighestAltitudeService service, ShareStatusService shareStatus, IConnectionProvider connections)
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
        var models = await service.GetListAsync(userId, connectedUsers);
        return Ok(models);
    }
}
