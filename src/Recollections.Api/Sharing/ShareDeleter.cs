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
    }
}
