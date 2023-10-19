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

    public async Task<IReadOnlyList<string>> GetUserIdsWithReaderToAsync(string userId)
    {
        // Permission
        // 0 = Read
        // 1 = CoOwner

        // State
        // 2 = Active

        return await db.Connections
            .Where(c => ((c.UserId == userId && (c.OtherPermission == 0 || c.OtherPermission == 1)) || (c.OtherUserId == userId && (c.Permission == 0 || c.Permission == 1))) && c.State == 2)
            .Select(c => c.UserId == userId ? c.OtherUserId : c.UserId)
            .ToListAsync();
    }

    public async Task<int?> GetPermissionAsync(string accessingUserId, string ownerUserId)
    {
        return await db.Connections
            .Where(c => ((c.UserId == accessingUserId && c.OtherUserId == ownerUserId) || (c.UserId == ownerUserId && c.OtherUserId == accessingUserId)) && c.State == 2)
            .Select(c => c.UserId == accessingUserId ? c.OtherPermission : c.Permission)
            .SingleOrDefaultAsync();
    }
}