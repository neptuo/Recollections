using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries
{
    public class TimelineListResponse
    {
        public List<TimelineEntryModel> Entries { get; set; }
        public bool HasMore { get; set; }

        public TimelineListResponse()
        {
        }

        public TimelineListResponse(List<TimelineEntryModel> entries, bool hasMore)
        {
            Ensure.NotNull(entries, "entries");
            Entries = entries;
            HasMore = hasMore;
        }
    }
}
