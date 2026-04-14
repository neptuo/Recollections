using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class Entry : IOwnerByUser, ISharingInherited
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Title { get; set; }
        public string Text { get; set; }

        public Story Story { get; set; }
        public StoryChapter Chapter { get; set; }

        public IList<OrderedLocation> Locations { get; set; } = new List<OrderedLocation>();
        public string TrackData { get; set; }
        public int? TrackPointCount { get; set; }
        public double? TrackLatitude { get; set; }
        public double? TrackLongitude { get; set; }
        public double? TrackAltitude { get; set; }
        public IList<Being> Beings { get; set; } = new List<Being>();

        public DateTime When { get; set; }
        public DateTime Created { get; set; }

        public bool IsSharingInherited { get; set; }
    }
}
