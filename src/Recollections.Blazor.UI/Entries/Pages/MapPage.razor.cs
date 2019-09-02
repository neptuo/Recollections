using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public class MapPageModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        protected List<MapEntryModel> Entries { get; set; } = new List<MapEntryModel>();
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        protected async override Task OnInitAsync()
        {
            await base.OnInitAsync();
            await UserState.EnsureAuthenticatedAsync();

            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Entries = await Api.GetMapListAsync();

            Markers.Clear();
            foreach (var entry in Entries)
            {
                Markers.Add(new MapMarkerModel()
                {
                    Latitude = entry.Location.Latitude,
                    Longitude = entry.Location.Longitude,
                    Altitude = entry.Location.Altitude,
                    Title = entry.Title
                });
            }
        }

        protected void OnMarkerSelected(int index) 
            => Navigator.OpenEntryDetail(Entries[index].Id);
    }
}
