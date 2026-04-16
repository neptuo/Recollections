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
    public partial class MapPage : IAsyncDisposable
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

        protected EntryListModel SelectedEntry { get; set; }
        protected EntryCardPopover entryPopover;
        protected Map mapComponent;
        private int selectedMarkerIndex = -1;
        private bool showPopoverPending;

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
                    Title = entry.Entry.Title
                });
            }

            IsLoading = false;
        }

        protected async Task OnMarkerSelectedAsync(int index)
        {
            await entryPopover.HideAsync();
            selectedMarkerIndex = index;
            SelectedEntry = Entries[index].Entry;
            showPopoverPending = true;
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (showPopoverPending && SelectedEntry != null && selectedMarkerIndex >= 0)
            {
                showPopoverPending = false;
                await mapComponent.ShowMarkerPopoverAsync(selectedMarkerIndex, entryPopover.ContentRef);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await entryPopover.HideAsync();
        }
    }
}
