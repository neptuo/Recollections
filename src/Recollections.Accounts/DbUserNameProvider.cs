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

            var filter = userNames.Distinct().ToArray();

            var users = await db.Users
                .Where(u => filter.Contains(u.UserName))
                .Select(u => new
                {
                    u.Id,
                    u.UserName
                })
                .ToListAsync();

            if (users.Count != filter.Length)
                throw Ensure.Exception.InvalidOperation($"Enumeration of userNames contains some not valid '{String.Join(", ", userNames)}'.");

            List<string> userIds = new List<string>();
            foreach (var userName in userNames)
                userIds.Add(users.First(u => u.UserName == userName).Id);

            return userIds;
        }

        public async Task<IReadOnlyList<string>> GetUserNamesAsync(IReadOnlyCollection<string> userIds)
        {
            if (userIds.Count == 0)
                return Array.Empty<string>();

            var filter = userIds.Distinct().ToArray();

            var users = await db.Users
                .Where(u => filter.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.UserName
                })
                .ToListAsync();

            if (users.Count != filter.Length)
                throw Ensure.Exception.InvalidOperation($"Enumeration of userIds contains some not valid '{String.Join(", ", userIds)}'.");

            List<string> userNames = new List<string>();
            foreach (var userId in userIds)
                userNames.Add(users.First(u => u.Id == userId).UserName);

            return userNames;
        }
    }
}
