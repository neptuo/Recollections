using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Accounts.Controllers;

[ApiController]
[Route("api/profiles/{id}")]
public class ProfileEntriesController(EntriesDataContext db, ShareStatusService shareStatus, StoryListMapper storyMapper, HighestAltitudeService altitudeService, IConnectionProvider connections)
    : Entries.Controllers.ControllerBase(db, shareStatus)
{
    [HttpGet("stories")]
    [ProducesDefaultResponseType(typeof(List<StoryListModel>))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetStories(string id) => RunBeingAsync(id, Permission.Read, async _ =>
    {
        var userId = User.FindUserId();
        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);

        var storyIds = shareStatus.OwnedByOrExplicitlySharedWithUser(
                db,
                db.Entries
                    .Where(e => e.UserId == id)
                    .Where(e => e.Story != null || e.Chapter != null),
                [userId, ShareStatusService.PublicUserId],
                connectedUsers
            )
            .Select(e => e.Story != null ? e.Story.Id : e.Chapter.Story.Id)
            .Distinct();

        var stories = await db.Stories
            .Where(s => storyIds.Contains(s.Id))
            .ToListAsync();

        var models = await storyMapper.MapAsync(stories, userId, connectedUsers);

        return Ok(models);
    });

    [HttpGet("highest-altitude")]
    [ProducesDefaultResponseType(typeof(List<EntryListModel>))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> GetHighestAltitude(string id) => RunBeingAsync(id, Permission.Read, async _ =>
    {
        var userId = User.FindUserId();
        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        var models = await altitudeService.GetListAsync(
            db.Entries.Where(e => e.UserId == id),
            [userId, ShareStatusService.PublicUserId],
            userId,
            connectedUsers
        );
        return Ok(models);
    });
}
