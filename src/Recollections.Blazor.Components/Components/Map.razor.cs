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
    public partial class Map : ComponentBase
    {
        [Inject]
        protected MapInterop Interop { get; set; }

        [Inject]
        protected ILog<Map> Log { get; set; }

        [Parameter]
        public IList<MapMarkerModel> Markers { get; set; }

        [Parameter]
        public Action MarkersChanged { get; set; }

        [Parameter]
        public Action<int> MarkerSelected { get; set; }

        [Parameter]
        public bool IsAdditive { get; set; }

        [Parameter]
        public bool IsResizable { get; set; }

        internal ElementReference Container { get; set; }
        internal bool IsZoomed { get; private set; }

        protected Modal SearchModal { get; set; }
        protected ElementReference SearchInput { get; set; }
        protected string SearchQuery { get; set; }
        protected List<MapSearchModel> SearchResults { get; } = new List<MapSearchModel>();

        public async override Task SetParametersAsync(ParameterView parameters)
        {
            Log.Debug("SetParametersAsync");
            await base.SetParametersAsync(parameters);

            Log.Debug($"Markers: '{Markers.Count}', has '{parameters.TryGetValue<IList<MapMarkerModel>>(nameof(Markers), out _)}'");
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            Log.Debug("OnAfterRenderAsync");

            await base.OnAfterRenderAsync(firstRender);
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

        protected async Task SearchLocationAsync()
        {
            SearchModal.Show();
            SearchResults.Clear();
            if (!String.IsNullOrEmpty(SearchQuery))
            {
                var results = await Interop.SearchAsync(SearchQuery);
                SearchResults.AddRange(results);

                Log.Debug($"Search, results: {SearchResults.Count}.");

                StateHasChanged();
            }
        }

        protected async ValueTask SearchResultSelectedAsync(MapSearchModel selected)
        {
            if (selected != null)
                await Interop.CenterAtAsync(selected.Latitude, selected.Longitude);

            SearchModal.Hide();
        }
    }
}
