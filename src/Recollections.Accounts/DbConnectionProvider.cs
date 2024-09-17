using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts;

public class DbConnectionProvider : IConnectionProvider
{
    private readonly DataContext db;

    public DbConnectionProvider(DataContext db)
    {
        Ensure.NotNull(db, "db");
        this.db = db;
    }

    public async Task<ConnectedUsersModel> GetConnectedUsersForAsync(string userId)
    {
        // Permission
        // 0 = Read
        // 1 = CoOwner

        // State
        // 2 = Active

        var activeConnections = await db.Connections
            .Where(c => (c.UserId == userId || c.OtherUserId == userId) && c.State == 2)
            .Select(c => c.UserId == userId ? new { UserId = c.OtherUserId, Permission = c.OtherPermission } : new { UserId = c.UserId, Permission = c.Permission })
            .ToListAsync();

        return new(
            activeConnections.Select(c => c.UserId).ToList(), 
            activeConnections.Where(c => c.Permission == 0 || c.Permission == 1).Select(c => c.UserId).ToList()
        );

        //return await db.Connections
        //    .Where(c => ((c.UserId == userId && (c.OtherPermission == 0 || c.OtherPermission == 1)) || (c.OtherUserId == userId && (c.Permission == 0 || c.Permission == 1))) && c.State == 2)
        //    .Select(c => c.UserId == userId ? c.OtherUserId : c.UserId)
        //    .ToListAsync();
    }

    public async Task<int?> GetPermissionAsync(string accessingUserId, string ownerUserId)
    {
        return await db.Connections
            .Where(c => ((c.UserId == accessingUserId && c.OtherUserId == ownerUserId) || (c.UserId == ownerUserId && c.OtherUserId == accessingUserId)) && c.State == 2)
            .Select(c => c.UserId == accessingUserId ? c.OtherPermission : c.Permission)
            .SingleOrDefaultAsync();
    }
}