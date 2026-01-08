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
[Route("api/stories/{storyId}/map")]
public class StoryMapController(MapService mapService, DataContext db, ShareStatusService shareStatus, IConnectionProvider connections)
    : ControllerBase(db, shareStatus)
{
    [HttpGet]
    public Task<IActionResult> List(string storyId) => RunStoryAsync(storyId, Permission.Read, async entry =>
    {
        string userId = HttpContext.User.FindUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var connectedUsers = await connections.GetConnectedUsersForAsync(userId);
        var results = await mapService.GetAsync(
            db.Entries.Where(e => e.Story.Id == storyId || e.Chapter.Story.Id == storyId), 
            userId, 
            connectedUsers
        );
        return Ok(results);
    });
}