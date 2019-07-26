using Microsoft.EntityFrameworkCore;
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
    [Owned]
    public class OrderedLocation : IEquatable<OrderedLocation>
    {
        public int Order { get; set; }

        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public double? Altitude { get; set; }

        public OrderedLocation()
        { }

        public OrderedLocation(int order, Location source)
        {
            Order = order;
            Longitude = source.Longitude;
            Latitude = source.Latitude;
            Altitude = source.Altitude;
        }

        public override bool Equals(object obj) => Equals(obj as OrderedLocation);

        public bool Equals(OrderedLocation other) => other != null &&
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
