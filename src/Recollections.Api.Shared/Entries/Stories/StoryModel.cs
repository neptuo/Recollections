using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Stories
{
    public class StoryModel
    {
        public string UserId { get; set; }

        public string Id { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }

        public List<ChapterModel> Chapters { get; set; } = new List<ChapterModel>();
    }
}
