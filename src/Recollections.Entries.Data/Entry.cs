using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class Entry
    {
        [Key]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Title { get; set; }
        public string Text { get; set; }

        public StoryChapter Chapter { get; set; }

        public IList<OrderedLocation> Locations { get; set; } = new List<OrderedLocation>();

        public DateTime When { get; set; }
        public DateTime Created { get; set; }
    }
}
