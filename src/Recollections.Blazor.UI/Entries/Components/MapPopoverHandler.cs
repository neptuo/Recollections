using Neptuo.Recollections.Components;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class MapPopoverHandler
    {
        private int selectedMarkerIndex = -1;
        private bool showPopoverPending;

        public EntryListModel SelectedEntry { get; private set; }

        public async Task SelectAsync(int markerIndex, EntryListModel entry, EntryCardPopover popover)
        {
            await popover.HideAsync();
            selectedMarkerIndex = markerIndex;
            SelectedEntry = entry;
            showPopoverPending = true;
        }

        public async Task TryShowPopoverAsync(Map map, EntryCardPopover popover)
        {
            if (showPopoverPending)
            {
                showPopoverPending = false;

                if (SelectedEntry != null && selectedMarkerIndex >= 0 && map != null)
                {
                    await map.ShowMarkerPopoverAsync(selectedMarkerIndex, popover.ContentRef);
                }
            }
        }

        public async ValueTask DisposeAsync(EntryCardPopover popover)
        {
            await popover.HideAsync();
        }
    }
}
