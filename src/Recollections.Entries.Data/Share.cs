using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class Share
    {
        public string UserId { get; set; }
        public int Permission { get; set; }

        public string EntryId { get; set; }
        public string StoryId { get; set; }
        public string ProfileUserId { get; set; }
    }
}
