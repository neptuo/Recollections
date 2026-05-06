using Neptuo.Recollections.Components;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class MediaPopoverHandler
    {
        private int selectedMarkerIndex = -1;
        private bool showPopoverPending;

        public MediaModel SelectedMedia { get; private set; }

        public async Task SelectAsync(int markerIndex, MediaModel media, MediaCardPopover popover)
        {
            await popover.HideAsync();
            selectedMarkerIndex = markerIndex;
            SelectedMedia = media;
            showPopoverPending = true;
        }

        public async Task TryShowPopoverAsync(Map map, MediaCardPopover popover)
        {
            if (showPopoverPending)
            {
                showPopoverPending = false;

                if (SelectedMedia != null && selectedMarkerIndex >= 0 && map != null && popover.ContentRef.Id != null)
                {
                    await map.ShowMarkerPopoverAsync(selectedMarkerIndex, popover.ContentRef);
                }
            }
        }

        public async ValueTask DisposeAsync(MediaCardPopover popover)
        {
            if (popover != null)
                await popover.HideAsync();
        }
    }
}
