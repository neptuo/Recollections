using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    [Owned]
    public class ImageLocation : IEquatable<ImageLocation>
    {
        public string ImageId { get; set; }

        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public double? Altitude { get; set; }

        public bool HasValue() => Longitude != null && Latitude != null;

        public override bool Equals(object obj) => Equals(obj as ImageLocation);

        public bool Equals(ImageLocation other) => other != null &&
            EqualityComparer<double?>.Default.Equals(Longitude, other.Longitude) &&
            EqualityComparer<double?>.Default.Equals(Latitude, other.Latitude);

        public override int GetHashCode()
        {
            var hashCode = 1209561475;
            hashCode = hashCode * -1521134295 + EqualityComparer<double?>.Default.GetHashCode(Longitude);
            hashCode = hashCode * -1521134295 + EqualityComparer<double?>.Default.GetHashCode(Latitude);
            return hashCode;
        }
    }
}
