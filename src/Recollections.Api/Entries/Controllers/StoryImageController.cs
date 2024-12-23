using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Controllers;

[ApiController]
[Route("api/stories/{storyId}/images")]
public class StoryImageController(ImageService service, DataContext db, ShareStatusService shareStatus, IConnectionProvider connections)
    : ControllerBase(db, shareStatus)
{
    [HttpGet]
    public Task<IActionResult> List(string storyId) => RunStoryAsync(storyId, Permission.Read, async entry =>
    {
        var userId = User.FindUserId();
        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        var query = shareStatus.OwnedByOrExplicitlySharedWithUser(
            db, 
            db.Entries.Where(e => e.Story.Id == storyId || e.Chapter.Story.Id == storyId), 
            [userId, ShareStatusService.PublicUserId], 
            connectedUsers
        );

        List<Image> entities = await db.Images
            .Where(i => query.Any(e => e.Id == i.Entry.Id))
            .Include(i => i.Entry)
            .OrderBy(i => i.When)
            .ToListAsync();

        var result = new List<EntryImagesModel>();
        foreach (var entryImages in entities.GroupBy(i => i.Entry.Id))
        {
            var entryModel = new EntryImagesModel();
            result.Add(entryModel);
            entryModel.EntryId = entryImages.Key;
            service.MapEntitiesToModels(entryImages, entryModel.Images, entry.UserId);
        }

        return Ok(result);
    });
}