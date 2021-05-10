using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class PoiToggleButton
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected PropertyCollection Properties { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        public bool IsEnabled { get; protected set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await UserState.EnsureAuthenticatedAsync();

            IsEnabled = await Properties.IsPointOfInterestAsync();
        }

        protected async Task OnToggleAsync()
        {
            if (await Navigator.AskAsync($"Turn {(IsEnabled ? "off" : "on")} point of interest layer? {Environment.NewLine}Requires reloading the application."))
            {
                await Properties.IsPointOfInterestAsync(IsEnabled = !IsEnabled);
                await Navigator.ReloadAsync();
            }
        }
    }
}
