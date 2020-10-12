using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Sharing.Controllers
{
    [ApiController]
    [Route("api")]
    public class ShareController : EntryControllerBase
    {
        private readonly DataContext db;
        private readonly IUserNameProvider userNames;

        public ShareController(DataContext db, IUserNameProvider userNames, ShareStatusService shareStatus)
            : base(db, shareStatus)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(userNames, "userNames");
            this.db = db;
            this.userNames = userNames;
        }

        private async Task<IActionResult> GetItemsAsync(IQueryable<ShareBase> query)
        {
            var items = await query
                .Select(s => new ShareModel()
                {
                    UserName = s.UserId,
                    Permission = (Permission)s.Permission
                })
                .ToListAsync();

            if (items.Count > 0)
            {
                var p = items.FirstOrDefault(s => s.UserName == ShareStatusService.PublicUserName);
                if (p != null)
                    items.Remove(p);

                var names = await userNames.GetUserNamesAsync(items.Select(s => s.UserName).ToArray());
                for (int i = 0; i < items.Count; i++)
                    items[i].UserName = names[i];

                items.Sort((a, b) => a.UserName.CompareTo(b.UserName));

                if (p != null)
                    items.Insert(0, p);
            }

            return Ok(items);
        }

        [HttpGet("entries/{entryId}/sharing")]
        [ProducesDefaultResponseType(typeof(ShareModel))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> GetEntryAsync(string entryId) => RunEntryAsync(entryId, entry =>
        {
            return GetItemsAsync(db.EntryShares.Where(s => s.EntryId == entryId));
        });

        private async Task<IActionResult> CreateAsync<T>(ShareModel model, Func<string, IQueryable<T>> findQuery, Func<T> entityFactory)
            where T : ShareBase
        {
            string userId;
            if (model.UserName != null && model.UserName != ShareStatusService.PublicUserName)
                userId = (await userNames.GetUserIdsAsync(new[] { model.UserName })).First();
            else
                userId = ShareStatusService.PublicUserId;

            T entity = await findQuery(userId).FirstOrDefaultAsync();
            if (entity == null)
            {
                entity = entityFactory();
                entity.UserId = userId;
                entity.Permission = (int)model.Permission;

                await db.Set<T>().AddAsync(entity);
            }
            else
            {
                entity.Permission = (int)model.Permission;
            }

            await db.SaveChangesAsync();
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPost("entries/{entryId}/sharing")]
        public Task<IActionResult> CreateEntryAsync(string entryId, ShareModel model) => RunEntryAsync(entryId, entry =>
        {
            return CreateAsync(
                model, 
                userId => db.EntryShares.Where(s => s.EntryId == entryId && s.UserId == userId), 
                () => new EntryShare(entryId)
            );
        });

        private async Task<IActionResult> DeleteAsync<T>(string userName, Func<string, IQueryable<T>> findQuery)
            where T : ShareBase
        {
            string userId;
            if (userName != null && userName != ShareStatusService.PublicUserName)
                userId = (await userNames.GetUserIdsAsync(new[] { userName })).First();
            else
                userId = ShareStatusService.PublicUserId;

            T entity = await findQuery(userId).FirstOrDefaultAsync();
            if (entity == null)
                return NotFound();

            db.Set<T>().Remove(entity);
            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("entries/{entryId}/sharing/{userName}")]
        public Task<IActionResult> DeleteEntryAsync(string entryId, string userName) => RunEntryAsync(entryId, async entry =>
        {
            return await DeleteAsync(
                userName, 
                userId => db.EntryShares.Where(s => s.EntryId == entryId && s.UserId == userId)
            );
        });
    }
}
