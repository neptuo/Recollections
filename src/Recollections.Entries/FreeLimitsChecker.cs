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
    public class FreeLimitsChecker
    {
        private readonly DataContext db;
        private readonly IUserPremiumProvider premiumProvider;
        private readonly FreeLimitsOptions options;

        public FreeLimitsChecker(DataContext db, IUserPremiumProvider premiumProvider, IOptions<FreeLimitsOptions> options)
        {
            Ensure.NotNull(db, "db");
            Ensure.NotNull(premiumProvider, "premiumProvider");
            Ensure.NotNull(options, "options");
            this.db = db;
            this.premiumProvider = premiumProvider;
            this.options = options.Value;
        }

        private async Task<bool> CountCheckAsync<T>(string userId, IQueryable<T> query, int? maxCount)
        {
            if (maxCount == null || await premiumProvider.HasPremiumAsync(userId))
                return true;

            int count = await query.CountAsync();
            return maxCount > count;
        }

        private async Task<bool> CountCheckAsync(string userId, int currentCount, int? maxCount)
        {
            if (maxCount == null || await premiumProvider.HasPremiumAsync(userId))
                return true;

            return maxCount >= currentCount;
        }

        private async Task<bool> BoolCheckAsync(string userId, bool? value)
        {
            if (value == null || await premiumProvider.HasPremiumAsync(userId))
                return true;

            return value.Value;
        }

        public Task<bool> CanCreateEntryAsync(string userId) 
            => CountCheckAsync(userId, db.Entries.Where(e => e.UserId == userId), options.EntryCount);

        public Task<bool> CanCreateImageAsync(string userId, string entryId)
            => CountCheckAsync(userId, db.Images.Where(i => i.Entry.Id == entryId), options.ImageInEntryCount);

        public Task<bool> CanSetGpsAsync(string userId, int gpsCount)
            => CountCheckAsync(userId, gpsCount, options.GpsInEntryCount);

        public Task<bool> IsOriginalImageStoredAsync(string userId)
            => BoolCheckAsync(userId, options.IsOriginalImageStored);

        public Task<bool> CanCreateStoryAsync(string userId)
            => CountCheckAsync(userId, db.Stories.Where(e => e.UserId == userId), options.StoryCount);
    }
}
