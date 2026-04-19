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
        private const string MapType = "Map.Type";

        public static Task<string> MapTypeAsync(this PropertyCollection properties) 
            => properties.GetAsync(MapType, "basic");

        public static Task MapTypeAsync(this PropertyCollection properties, string type) 
            => properties.SetAsync(MapType, type);

        
        private const string Theme = "App.Theme";

        public static Task<ThemeType> ThemeAsync(this PropertyCollection properties) 
            => properties.GetAsync(Theme, ThemeType.Auto);

        public static Task ThemeAsync(this PropertyCollection properties, ThemeType isEnabled) 
            => properties.SetAsync(Theme, isEnabled);
    }
}
