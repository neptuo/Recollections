using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class MapModel : ComponentBase
    {
        [Inject]
        protected MapInterop Interop { get; set; }

        [Parameter]
        internal protected int Zoom { get; set; } = 10;

        [Parameter]
        internal protected IList<LocationModel> Markers { get; set; }

        [Parameter]
        protected Action MarkersChanged { get; set; }

        [Parameter]
        protected bool IsAdditive { get; set; }

        internal ElementRef Container { get; set; }

        internal bool IsEditable => MarkersChanged != null;

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();
            await Interop.InitializeAsync(this);

            Console.WriteLine("Map.OnAfterRenderAsync");
        }

        internal void MoveMarker(int? index, double latitude, double longitude, double? altitude)
        {
            Console.WriteLine($"MoveMarker: {index}, {latitude}, {longitude}, {altitude}");

            LocationModel marker = null;
            if (index == null)
                Markers.Add(marker = new LocationModel());
            else
                marker = Markers[index.Value];

            marker.Latitude = latitude;
            marker.Longitude = longitude;
            marker.Altitude = altitude;
            MarkersChanged?.Invoke();
        }
    }
}
