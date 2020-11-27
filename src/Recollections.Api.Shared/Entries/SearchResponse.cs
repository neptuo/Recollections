using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class SearchResponse
    {
        public List<SearchEntryModel> Entries { get; set; }
        public bool HasMore { get; set; }

        public SearchResponse()
        {
        }

        public SearchResponse(List<SearchEntryModel> entries, bool hasMore)
        {
            Ensure.NotNull(entries, "entries");
            Entries = entries;
            HasMore = hasMore;
        }
    }
}
