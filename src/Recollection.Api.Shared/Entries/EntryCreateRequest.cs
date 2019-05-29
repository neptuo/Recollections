using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries
{
    public class EntryCreateRequest
    {
        public string Title { get; set; }
        public DateTime When { get; set; }

        public EntryCreateRequest()
        { }

        public EntryCreateRequest(string title, DateTime when)
        {
            Ensure.NotNull(title, "title");
            Title = title;
            When = when;
        }
    }
}
