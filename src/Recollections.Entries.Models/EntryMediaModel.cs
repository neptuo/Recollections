using System.Collections.Generic;

namespace Neptuo.Recollections.Entries
{
    public class EntryMediaModel
    {
        public string EntryId { get; set; }
        public List<MediaModel> Media { get; set; } = new List<MediaModel>();
    }
}
