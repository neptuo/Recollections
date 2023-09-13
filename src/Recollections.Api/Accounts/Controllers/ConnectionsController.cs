using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Controllers;

// State 3 = Rejected by acceptor
// State 4 = Rejected by initiator

[ApiController]
[Route("api/accounts/connections")]
public class ConnectionsController : ControllerBase
{
    private readonly DataContext db;

    public ConnectionsController(DataContext db)
    {
        Ensure.NotNull(db, "db");
        this.db = db;
    }

    [HttpGet]
    [ProducesDefaultResponseType(typeof(List<ConnectionModel>))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetListAsync()
    {
        string userId = HttpContext.User.FindUserId();
        if (userId == null)
            return Unauthorized();

        List<ConnectionModel> result = await db.Connections
            .Where(c => c.UserId == userId || c.OtherUserId == userId)
            .Select(c => new ConnectionModel() 
            {
                OtherUserName = c.UserId == userId ? c.OtherUser.UserName : c.User.UserName,
                Role = c.UserId == userId ? ConnectionRole.Initiator : ConnectionRole.Acceptor,
                State = c.State == 1 
                    ? ConnectionState.Pending
                    : c.State == 2
                        ? ConnectionState.Active
                        : ConnectionState.Rejected
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync(ConnectionModel model)
    {
        Ensure.NotNull(model, "model");

        string userId = HttpContext.User.FindUserId();
        if (userId == null)
            return Unauthorized();

        if (String.IsNullOrEmpty(model.OtherUserName))
            return BadRequest();

        model.OtherUserName = model.OtherUserName.Trim();

        bool connectionExists = await db.Connections.AnyAsync(c => (c.UserId == userId && c.OtherUser.UserName == model.OtherUserName) || (c.OtherUserId == userId && c.User.UserName == model.OtherUserName));
        if (connectionExists)
            return BadRequest();

        string otherUserId = await db.Users.Where(u => u.UserName == model.OtherUserName).Select(u => u.Id).SingleOrDefaultAsync();
        if (String.IsNullOrEmpty(otherUserId))
            return BadRequest();

        var entity = new UserConnection() 
        {
            UserId = userId,
            OtherUserId = otherUserId,
            State = 1
        };

        db.Connections.Add(entity);
        await db.SaveChangesAsync();

        return Ok();
    }

    protected async Task<IActionResult> RunConnectionAsync(string otherUserName, Func<UserConnection, Task<IActionResult>> handler)
    {
        Ensure.NotNullOrEmpty(otherUserName, "otherUserName");

        string userId = HttpContext.User.FindUserId();
        if (userId == null)
            return Unauthorized();

        var entity = await db.Connections.SingleOrDefaultAsync(c => (c.UserId == userId && c.OtherUser.UserName == otherUserName) || (c.OtherUserId == userId && c.User.UserName == otherUserName));
        if (entity == null)
            return NotFound();

        if (entity.State == 3 && entity.OtherUserId != userId)
            return BadRequest();

        if (entity.State == 4 && entity.UserId != userId)
            return BadRequest();

        return await handler(entity);
    }

    [HttpPut]
    public Task<IActionResult> PutAsync(ConnectionModel model) => RunConnectionAsync(model?.OtherUserName, async entity =>
    {
        string userId = HttpContext.User.FindUserId();
        
        // Pending
        if (entity.State == 1) 
        {   
            if (model.State == ConnectionState.Active) 
            {
                if (entity.UserId == userId)
                    return BadRequest();

                entity.State = 2;
            }
            else if (model.State == ConnectionState.Rejected)
            {
                if (entity.UserId == userId)
                    entity.State = 4;
                else
                    entity.State = 3;
            }
        }
        // Active
        else if (entity.State == 2)
        {
            if (model.State == ConnectionState.Rejected)
            {
                // Reject
                if (entity.UserId == userId)
                    entity.State = 4;
                else
                    entity.State = 3;
            }
            else 
            {
                return BadRequest();
            }
        }
        // Rejected
        else if (entity.State == 3 || entity.State == 4) 
        {
            if (model.State == ConnectionState.Active) 
                entity.State = 2;
            else
                return BadRequest();
        }
        
        await db.SaveChangesAsync();

        return Ok();
    });
    
    [HttpDelete("{otherUserName}")]
    public Task<IActionResult> DeleteAsync(string otherUserName) => RunConnectionAsync(otherUserName, async entity =>
    {
        db.Connections.Remove(entity);
        await db.SaveChangesAsync();

        return Ok();
    });
}
