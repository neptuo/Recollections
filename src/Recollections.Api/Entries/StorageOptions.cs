using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class StorageOptions
    {
        public string PathTemplate { get; set; }

        public int MaxLength { get; set; }

        public List<string> SupportedExtensions { get; } = new List<string>();

        public string GetPath(string userId, string entryId) => PathTemplate
            .Replace("{UserId}", userId)
            .Replace("{EntryId}", entryId);

        public bool IsSupportedExtension(string extension)
        {
            if (SupportedExtensions == null || SupportedExtensions.Count == 0)
                return false;

            return SupportedExtensions.Contains(extension);
        }
    }
}
