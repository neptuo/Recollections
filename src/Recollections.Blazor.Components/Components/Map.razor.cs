using Microsoft.AspNetCore.Components;
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
        internal protected IList<MapMarkerModel> Markers { get; set; }

        [Parameter]
        protected Action MarkersChanged { get; set; }

        [Parameter]
        internal protected Action<int> MarkerSelected { get; set; }

        [Parameter]
        protected bool IsAdditive { get; set; }

        [Parameter]
        internal protected bool IsResizable { get; set; }

        internal ElementRef Container { get; set; }

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();
            await Interop.InitializeAsync(this);
        }

        internal void MoveMarker(int? index, double latitude, double longitude, double? altitude)
        {
            Console.WriteLine($"MoveMarker: {index?.ToString() ?? "<null>"}, {latitude}, {longitude}, {altitude}");

            MapMarkerModel marker;
            if (index == null)
            {
                Markers.Add(marker = new MapMarkerModel()
                {
                    IsEditable = true
                });
            }
            else
            {
                marker = Markers[index.Value];
            }

            if (marker.IsEditable)
            {
                marker.Latitude = latitude;
                marker.Longitude = longitude;
                marker.Altitude = altitude;
                MarkersChanged?.Invoke();
            }
        }
    }
}
