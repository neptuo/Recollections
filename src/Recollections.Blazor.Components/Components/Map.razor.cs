using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
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

        [Inject]
        protected ILog<MapModel> Log { get; set; }

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
        internal bool IsZoomed { get; private set; }

        public async override Task SetParametersAsync(ParameterCollection parameters)
        {
            Log.Debug("SetParametersAsync");
            await base.SetParametersAsync(parameters);

            Log.Debug($"Markers: '{Markers.Count}', has '{parameters.TryGetValue<IList<MapMarkerModel>>(nameof(Markers), out _)}'");
        }

        protected async override Task OnAfterRenderAsync()
        {
            Log.Debug("OnAfterRenderAsync");

            await base.OnAfterRenderAsync();
            await Interop.InitializeAsync(this);

            if (Markers.Count > 0)
                IsZoomed = true;
        }

        internal void MoveMarker(int? index, double latitude, double longitude, double? altitude)
        {
            Log.Debug($"MoveMarker: {index?.ToString() ?? "<null>"}, {latitude}, {longitude}, {altitude}");

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
