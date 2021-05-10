using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public static class PropertyCollectionExtensions
    {
        private const string PointOfInterest = "Map.PointOfInterest";

        public static Task<bool> IsPointOfInterestAsync(this PropertyCollection properties) 
            => properties.GetAsync(PointOfInterest, false);

        public static Task IsPointOfInterestAsync(this PropertyCollection properties, bool isEnabled) 
            => properties.SetAsync(PointOfInterest, isEnabled);
    }
}
