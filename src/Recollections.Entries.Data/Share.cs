using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public abstract class ShareBase
    {
        public string UserId { get; set; }
        public int Permission { get; set; }
    }

    public class EntryShare : ShareBase
    {
        public string EntryId { get; set; }
    }

    public class StoryShare : ShareBase
    {
        public string StoryId { get; set; }
    }

    public class ProfileShare : ShareBase
    {
        public string ProfileUserId { get; set; }
    }
}
