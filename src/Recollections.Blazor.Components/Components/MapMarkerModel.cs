using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class MapMarkerModel : IEquatable<MapMarkerModel>
    {
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public double? Altitude { get; set; }

        public bool IsEditable { get; set; }
        public string Title { get; set; }
        public string DropColor { get; set; }

        public override bool Equals(object? obj)
            => base.Equals(obj as MapMarkerModel);

        public bool Equals(MapMarkerModel other)
        {
            if (other == null)
                return false;

            return Longitude == other.Longitude
                && Latitude == other.Latitude
                && Altitude == other.Altitude
                && IsEditable == other.IsEditable
                && Title == other.Title
                && DropColor == other.DropColor;
        }

        public override int GetHashCode()
            => HashCode.Combine(
                Longitude,
                Latitude,
                Altitude,
                IsEditable,
                Title,
                DropColor
            );
    }
}
