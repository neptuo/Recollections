using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class MapMarkerModel
    {
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public double? Altitude { get; set; }

        public bool IsEditable { get; set; }
        public string Title { get; set; }
        public string DropColor { get; set; }
    }
}
