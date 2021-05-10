using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class PoiToggleButton : IMapToggleButton
    {
        private readonly Navigator navigator;
        private readonly PropertyCollection properties;
        private readonly UserState userState;

        public string IconIdentifier => "place-of-worship";
        public string Title => (IsEnabled ? "Hide" : "Show") + " points of interest";
        public bool IsEnabled { get; private set; }

        public PoiToggleButton(Navigator navigator, PropertyCollection properties, UserState userState)
        {
            this.navigator = navigator;
            this.properties = properties;
            this.userState = userState;
        }

        public async Task InitializeAsync()
        {
            await userState.EnsureAuthenticatedAsync();
            IsEnabled = await properties.IsPointOfInterestAsync();
        }

        public async Task OnClickAsync()
        {
            if (await navigator.AskAsync($"Turn {(IsEnabled ? "off" : "on")} point of interest layer? {Environment.NewLine}Requires reloading the application."))
            {
                await properties.IsPointOfInterestAsync(IsEnabled = !IsEnabled);
                await navigator.ReloadAsync();
            }
        }
    }
}
