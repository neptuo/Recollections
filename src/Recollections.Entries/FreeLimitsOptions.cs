using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class FreeLimitsOptions
    {
        public int? EntryCount { get; set; }
        public int? ImageInEntryCount { get; set; }
        public int? VideoInEntryCount { get; set; }
        public int? GpsInEntryCount { get; set; }
        public bool? IsOriginalImageStored { get; set; }
        public int? StoryCount { get; set; }
        public int? BeingCount { get; set; }
    }
}
