using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class Story : IOwnerByUser, ISharingInherited
    {
        [Key]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Title { get; set; }
        public string Text { get; set; }

        public List<StoryChapter> Chapters { get; set; } = new List<StoryChapter>();

        public DateTime Created { get; set; }

        public bool IsSharingInherited { get; set; }
    }
}
