using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class ImageModel
    {
        public string Id { get; set; }

        public string Thumbnail { get; set; }
        public string Preview { get; set; }
        public string Original{ get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime When { get; set; }
    }
}
