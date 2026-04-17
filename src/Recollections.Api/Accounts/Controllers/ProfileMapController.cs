using Microsoft.AspNetCore.Mvc;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using System.Linq;
using System.Threading.Tasks;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Accounts.Controllers;

[ApiController]
[Route("api/profiles/{id}/map")]
public class ProfileMapController(MapService mapService, EntriesDataContext db, ShareStatusService shareStatus, IConnectionProvider connections)
    : Entries.Controllers.ControllerBase(db, shareStatus)
{
    [HttpGet]
    public Task<IActionResult> List(string id) => RunBeingAsync(id, Permission.Read, async _ =>
    {
        var userId = User.FindUserId();

        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        var results = await mapService.GetAsync(
            db.Entries.Where(e => e.UserId == id),
            [userId, ShareStatusService.PublicUserId],
            connectedUsers
        );
        return Ok(results);
    });
}
