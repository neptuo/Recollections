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
    public class ShareDeleter
    {
        private readonly DataContext db;

        public ShareDeleter(DataContext db)
        {
            Ensure.NotNull(db, "db");
            this.db = db;
        }

        public async Task DeleteEntrySharesAsync(string entryId)
        {
            var entities = await db.EntryShares
                .Where(s => s.EntryId == entryId)
                .ToListAsync();

            db.EntryShares.RemoveRange(entities);
            await db.SaveChangesAsync();
        }

        public async Task DeleteStorySharesAsync(string storyId)
        {
            var entities = await db.StoryShares
                .Where(s => s.StoryId == storyId)
                .ToListAsync();

            db.StoryShares.RemoveRange(entities);
            await db.SaveChangesAsync();
        }
    }
}
