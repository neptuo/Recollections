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
public class StoryMediaController(ImageService imageService, VideoService videoService, DataContext db, ShareStatusService shareStatus, IConnectionProvider connections)
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

        var entryIds = await query.Select(e => e.Id).ToListAsync();

        List<Image> images = await db.Images
            .Where(i => entryIds.Contains(i.Entry.Id))
            .Include(i => i.Entry)
            .OrderBy(i => i.When)
            .ToListAsync();

        List<Video> videos = await db.Videos
            .Where(v => entryIds.Contains(v.Entry.Id))
            .Include(v => v.Entry)
            .OrderBy(v => v.When)
            .ToListAsync();

        var result = new Dictionary<string, EntryMediaModel>();

        foreach (var image in images)
        {
            if (!result.TryGetValue(image.Entry.Id, out var entryModel))
                result[image.Entry.Id] = entryModel = new EntryMediaModel { EntryId = image.Entry.Id };

            var model = new ImageModel();
            imageService.MapEntityToModel(image, model, image.Entry.UserId);
            entryModel.Media.Add(new MediaModel { Type = "image", Image = model });
        }

        foreach (var video in videos)
        {
            if (!result.TryGetValue(video.Entry.Id, out var entryModel))
                result[video.Entry.Id] = entryModel = new EntryMediaModel { EntryId = video.Entry.Id };

            var model = new VideoModel();
            videoService.MapEntityToModel(video, model, video.Entry.UserId);
            entryModel.Media.Add(new MediaModel { Type = "video", Video = model });
        }

        foreach (var entryModel in result.Values)
            entryModel.Media = entryModel.Media.OrderBy(m => m.Type == "image" ? m.Image.When : m.Video.When).ToList();

        return Ok(result.Values.OrderBy(m => m.EntryId));
    });
}
