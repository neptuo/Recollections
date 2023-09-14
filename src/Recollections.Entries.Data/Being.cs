using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class Being : IOwnerByUser
    {
        [Key]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Name { get; set; }
        public string Icon { get; set; }
        public string Text { get; set; }

        public DateTime Created { get; set; }

        public IList<Entry> Entries { get; set; } = new List<Entry>();
    }
}
