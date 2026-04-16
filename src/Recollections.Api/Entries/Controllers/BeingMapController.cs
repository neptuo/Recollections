using Microsoft.AspNetCore.Mvc;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers;

[ApiController]
[Route("api/beings/{beingId}/map")]
public class BeingMapController(MapService mapService, DataContext db, ShareStatusService shareStatus, IConnectionProvider connections)
    : ControllerBase(db, shareStatus)
{
    [HttpGet]
    public Task<IActionResult> List(string beingId) => RunBeingAsync(beingId, Permission.Read, async being =>
    {
        var userId = User.FindUserId();

        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        var results = await mapService.GetAsync(
            db.Entries.Where(e => e.Beings.Any(b => b.Id == beingId)),
            userId,
            [userId, ShareStatusService.PublicUserId],
            connectedUsers
        );
        return Ok(results);
    });
}
