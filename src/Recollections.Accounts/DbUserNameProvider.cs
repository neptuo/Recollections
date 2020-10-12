using Microsoft.EntityFrameworkCore;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class DbUserNameProvider : IUserNameProvider
    {
        private readonly DataContext db;

        public DbUserNameProvider(DataContext db)
        {
            Ensure.NotNull(db, "db");
            this.db = db;
        }

        public async Task<IReadOnlyList<string>> GetUserIdsAsync(IReadOnlyCollection<string> userNames)
        {
            if (userNames.Count == 0)
                return Array.Empty<string>();

            List<string> userIds = await db.Users
                .Where(u => userNames.Contains(u.UserName))
                .Select(u => u.Id)
                .ToListAsync();

            if (userIds.Count != userNames.Count)
                throw Ensure.Exception.InvalidOperation($"Enumeration of userNames contains some not valid '{String.Join(", ", userNames)}'.");

            return userIds;
        }

        public async Task<IReadOnlyList<string>> GetUserNamesAsync(IReadOnlyCollection<string> userIds)
        {
            if (userIds.Count == 0)
                return Array.Empty<string>();

            List<string> userNames = await db.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => u.UserName)
                .ToListAsync();

            if (userNames.Count != userIds.Count)
                throw Ensure.Exception.InvalidOperation($"Enumeration of userIds contains some not valid '{String.Join(", ", userIds)}'.");

            return userNames;
        }
    }
}
