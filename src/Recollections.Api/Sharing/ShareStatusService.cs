using Microsoft.EntityFrameworkCore;
using Neptuo;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing
{
    public class ShareStatusService
    {
        public const string PublicUserId = "public";
        public const string PublicUserName = "public";

        private readonly DataContext db;

        public ShareStatusService(DataContext db)
        {
            Ensure.NotNull(db, "db");
            this.db = db;
        }

        public async Task<bool> IsEntrySharedForReadAsync(string entryId, string userId)
        {
            bool isAllowed = await db.EntryShares.AnyAsync(s => s.EntryId == entryId && (s.UserId == userId || s.UserId == PublicUserId));
            return isAllowed;
        }

        public async Task<bool> IsEntrySharedForWriteAsync(string entryId, string userId)
        {
            bool isAllowed = await db.EntryShares.AnyAsync(s => s.EntryId == entryId && (s.UserId == userId || s.UserId == PublicUserId) && s.Permission == (int)Permission.Write);
            return isAllowed;
        }
    }
}
