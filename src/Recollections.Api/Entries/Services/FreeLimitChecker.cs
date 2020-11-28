using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Recollections.Accounts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class FreeLimitChecker
    {
        private readonly DataContext db;
        private readonly IUserPremiumProvider premiumProvider;
        private readonly FreeLimitsOptions options;

        public FreeLimitChecker(DataContext db, IUserPremiumProvider premiumProvider, IOptions<FreeLimitsOptions> options)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(premiumProvider, "premiumProvider");
            Ensure.NotNull(options, "options");
            this.db = db;
            this.premiumProvider = premiumProvider;
            this.options = options.Value;
        }

        public async Task<bool> CanCreateEntryAsync(string userId)
        {
            if (options.EntryCount == null || await premiumProvider.HasPremiumAsync(userId))
                return true;

            int count = await db.Entries.CountAsync(e => e.UserId == userId);
            return options.EntryCount.Value > count;
        }
    }
}
