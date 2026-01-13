using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop.Implementation;
using Neptuo.Logging;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class Map(MapInterop Interop, ImageInterop ImageInterop, ILog<Map> Log, IMapService Service) : ComponentBase, IDisposable
    {
        [Parameter]
        public IList<MapMarkerModel> Markers { get; set; }

        [Parameter]
        public EventCallback MarkersChanged { get; set; }

        [Parameter]
        public EventCallback<int> MarkerSelected { get; set; }

        [Parameter]
        public EventCallback OnClearLocation { get; set; }

        [Parameter]
        public bool IsAdditive { get; set; }

        [CascadingParameter]
        public FormState FormState { get; set; }

        internal ElementReference Container { get; set; }
        internal bool IsZoomed { get; private set; }

        protected Modal SearchModal { get; set; }
        protected ElementReference SearchInput { get; set; }
        protected string SearchQuery { get; set; }
        protected List<MapSearchModel> SearchResults { get; } = [];
        protected bool HasSearchResultsChanged { get; set;}
        protected Modal TileTypeModal { get; set; }
        protected string TileType { get; set; }

        internal bool IsEditable => FormState?.IsEditable ?? true;

        protected bool IsClearable => Markers.Count == 1 
            && OnClearLocation.HasDelegate
            && Markers[0].IsEditable 
            && Markers[0].Latitude != null 
            && Markers[0].Longitude != null;

        protected bool IsInitialized { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            TileType = await Service.GetTypeAsync();

            IsInitialized = true;
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            Log.Debug("OnAfterRenderAsync");

            if (!IsInitialized)
                return;

            await base.OnAfterRenderAsync(firstRender);
            await Interop.InitializeAsync(this);

            if (Markers.Count > 0)
                IsZoomed = true;
        }

        protected override bool ShouldRender()
        {
            var result = Interop.ShouldRender() || HasSearchResultsChanged;
            HasSearchResultsChanged = false;
            Log.Debug($"ShouldRender: {result}");
            return result;
        }

        public void Dispose()
        {
            Log.Debug("Dispose");
        }

        internal async void MoveMarker(int? index, double latitude, double longitude)
        {
            if (!IsEditable)
                return;

            Log.Debug($"MoveMarker: {index?.ToString() ?? "<null>"}, {latitude}, {longitude}");

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
                await MarkersChanged.InvokeAsync();
            }
        }

        protected async Task SearchLocationAsync()
        {
            bool hadSearchResults = SearchResults.Count > 0;

            SearchModal.Show();
            SearchResults.Clear();
            if (!String.IsNullOrEmpty(SearchQuery))
            {
                var results = await Service.GetGeoLocateListAsync(SearchQuery);
                SearchResults.AddRange(results);

                Log.Debug($"Search, results: {SearchResults.Count}.");
            }
            
            HasSearchResultsChanged = hadSearchResults || SearchResults.Count > 0;
            StateHasChanged();
        }

        protected async Task SearchResultSelectedAsync(MapSearchModel selected)
        {
            SearchModal.Hide();
            
            Log.Debug($"Centering at selected location: {selected.Latitude}, {selected.Longitude}");
            await Interop.CenterAtAsync(selected.Latitude, selected.Longitude);
        }

        internal async Task LoadTileAsync(JSObjectReference img, int x, int y, int z)
        {
            var content = await Service.GetTileAsync(TileType, x, y, z);
            await ImageInterop.SetAsync(img, content);
        }

        protected async Task SelectTypeAsync(string type)
        {
            TileType = type;
            await Interop.RedrawAsync();
            await Service.SetTypeAsync(type);
            TileTypeModal.Hide();
        }
    }
}
