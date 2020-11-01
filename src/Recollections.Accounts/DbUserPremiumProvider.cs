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
    public class DbUserPremiumProvider : IUserPremiumProvider
    {
        private readonly DataContext db;

        public DbUserPremiumProvider(DataContext db)
        {
            Ensure.NotNull(db, "db");
            this.db = db;
        }

        public async Task<bool> HasPremiumAsync(string userId)
        {
            Ensure.NotNull(userId, "userId");
            return await db.Users.AnyAsync(u => u.Id == userId && u.EmailConfirmed);
        }
    }
}
