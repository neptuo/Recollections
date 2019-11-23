using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class EntryStoryUpdateModel
    {
        public string StoryId { get; set; }
        public string ChapterId { get; set; }

        public EntryStoryUpdateModel()
        {
        }

        public EntryStoryUpdateModel(string storyId)
        {
            StoryId = storyId;
        }
    }
}
