using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class SystemIoStorageOptions
    {
        public string PathTemplate { get; set; }

        public string GetPath(string userId, string entryId) => PathTemplate
            .Replace("{UserId}", userId)
            .Replace("{EntryId}", entryId);
    }
}
