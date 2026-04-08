using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers;

[ApiController]
[Route("api/stories/{storyId}/media")]
public class StoryMediaController(EntryMediaMapper entryMediaMapper, DataContext db, ShareStatusService shareStatus, IConnectionProvider connections)
    : ControllerBase(db, shareStatus)
{
    [HttpGet]
    public Task<IActionResult> List(string storyId) => RunStoryAsync(storyId, Permission.Read, async _ =>
    {
        var userId = User.FindUserId();
        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        var query = shareStatus.OwnedByOrExplicitlySharedWithUser(
            db,
            db.Entries.Where(e => e.Story.Id == storyId || e.Chapter.Story.Id == storyId),
            [userId, ShareStatusService.PublicUserId],
            connectedUsers
        );

        Dictionary<string, string> entryIdsWithUserIds = await query
            .Select(e => new { e.Id, e.UserId })
            .ToDictionaryAsync(e => e.Id, e => e.UserId);

        Dictionary<string, List<MediaModel>> mediaByEntryId = await entryMediaMapper.MapByEntryIdAsync(entryIdsWithUserIds);
        var result = mediaByEntryId
            .Where(item => item.Value.Count > 0)
            .Select(item => new EntryMediaModel
            {
                EntryId = item.Key,
                Media = item.Value
            })
            .OrderBy(m => m.EntryId);

        return Ok(result);
    });
}
