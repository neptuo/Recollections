using Neptuo;
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

        public EntryShare()
        { }

        public EntryShare(string entryId)
        {
            Ensure.NotNull(entryId, "entryId");
            EntryId = entryId;
        }
    }

    public class StoryShare : ShareBase
    {
        public string StoryId { get; set; }

        public StoryShare()
        { }

        public StoryShare(string storyId)
        {
            Ensure.NotNull(storyId, "storyId");
            StoryId = storyId;
        }
    }

    public class BeingShare : ShareBase
    {
        public string BeingId { get; set; }

        public BeingShare()
        { }

        public BeingShare(string beingId)
        {
            Ensure.NotNull(beingId, "beingId");
            BeingId = beingId;
        }
    }

    public class ProfileShare : ShareBase
    {
        public string ProfileUserId { get; set; }
    }
}
