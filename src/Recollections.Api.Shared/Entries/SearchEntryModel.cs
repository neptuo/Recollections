using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class SearchEntryModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime When { get; set; }

        public string Text { get; set; }

        public string StoryTitle { get; set; }
        public string ChapterTitle { get; set; }

        public int ImageCount { get; set; }
        public int GpsCount { get; set; }
    }
}
