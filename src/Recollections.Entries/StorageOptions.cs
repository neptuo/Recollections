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
        public MediaOptions Images { get; set; }
        public MediaOptions Videos { get; set; }
    }

    public class MediaOptions
    {
        public int MaxLength { get; set; }
        public int? PremiumMaxLength { get; set; }

        public List<string> SupportedExtensions { get; } = new List<string>();

        public bool IsSupportedExtension(string extension)
        {
            if (SupportedExtensions == null || SupportedExtensions.Count == 0)
                return false;

            return SupportedExtensions.Contains(extension);
        }
    }
}
