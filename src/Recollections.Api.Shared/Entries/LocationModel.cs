using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class LocationModel : ICloneable<LocationModel>, IEquatable<LocationModel>
    {
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public double? Altitude { get; set; }

        public LocationModel Clone() => new LocationModel()
        {
            Longitude = Longitude,
            Latitude = Latitude,
            Altitude = Altitude
        };

        public override bool Equals(object obj) => Equals(obj as LocationModel);

        public bool Equals(LocationModel other) => other != null &&
            EqualityComparer<double?>.Default.Equals(Longitude, other.Longitude) &&
            EqualityComparer<double?>.Default.Equals(Latitude, other.Latitude) &&
            EqualityComparer<double?>.Default.Equals(Altitude, other.Altitude);

        public override int GetHashCode()
        {
            var hashCode = 1101583378;
            hashCode = hashCode * -1521134295 + EqualityComparer<double?>.Default.GetHashCode(Longitude);
            hashCode = hashCode * -1521134295 + EqualityComparer<double?>.Default.GetHashCode(Latitude);
            hashCode = hashCode * -1521134295 + EqualityComparer<double?>.Default.GetHashCode(Altitude);
            return hashCode;
        }
    }
}
