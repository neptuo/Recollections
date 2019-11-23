using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class StoryChapter
    {
        [Key]
        public string Id { get; set; }

        public Story Story { get; set; }

        public int Order { get; set; }

        public string Title { get; set; }
        public string Text { get; set; }

        public DateTime Created { get; set; }
    }
}
