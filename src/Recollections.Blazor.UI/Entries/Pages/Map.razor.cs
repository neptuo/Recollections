using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class Map
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected PropertyCollection Properties { get; set; }

        protected List<MapEntryModel> Entries { get; set; } = new List<MapEntryModel>();
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        protected bool IsLoading { get; set; } = true;

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
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

            IsLoading = false;
        }

        protected void OnMarkerSelected(int index)
        {
            var entry = Entries[index];
            Navigator.OpenEntryDetail(entry.Id);
        }
    }
}
