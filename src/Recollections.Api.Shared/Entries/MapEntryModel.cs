using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class MapEntryModel
    {
        public string Id { get; set; }
        public string Title { get; set; }

        public LocationModel Location { get; set; }
    }
}
