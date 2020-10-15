using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Stories
{
    public class StoryListModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public string Id { get; set; }
        public string Title { get; set; }

        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }

        public int Chapters { get; set; }
        public int Entries { get; set; }
    }
}
