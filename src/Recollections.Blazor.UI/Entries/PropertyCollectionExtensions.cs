using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neptuo.Recollections.Accounts.Components;

namespace Neptuo.Recollections.Accounts
{
    public static class PropertyCollectionExtensions
    {
        private const string PointOfInterest = "Map.PointOfInterest";

        public static Task<bool> IsPointOfInterestAsync(this PropertyCollection properties) 
            => properties.GetAsync(PointOfInterest, false);

        public static Task IsPointOfInterestAsync(this PropertyCollection properties, bool isEnabled) 
            => properties.SetAsync(PointOfInterest, isEnabled);

        
        private const string Theme = "App.Theme";

        public static Task<ThemeType> ThemeAsync(this PropertyCollection properties) 
            => properties.GetAsync(Theme, ThemeType.Auto);

        public static Task ThemeAsync(this PropertyCollection properties, ThemeType isEnabled) 
            => properties.SetAsync(Theme, isEnabled);
    }
}
