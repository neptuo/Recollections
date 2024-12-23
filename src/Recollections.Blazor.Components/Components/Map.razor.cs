﻿using Microsoft.AspNetCore.Components;
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
    public partial class Map : ComponentBase, IDisposable
    {
        [Inject]
        protected MapInterop Interop { get; set; }

        [Inject]
        protected ImageInterop ImageInterop { get; set; }

        [Inject]
        protected ILog<Map> Log { get; set; }

        [Inject]
        internal IMapService Service { get; set; }

        [Parameter]
        public IList<MapMarkerModel> Markers { get; set; }

        [Parameter]
        public Action MarkersChanged { get; set; }

        [Parameter]
        public Action<int> MarkerSelected { get; set; }

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
        protected List<MapSearchModel> SearchResults { get; } = new List<MapSearchModel>();
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

        private MapSearchModel selected;

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            Log.Debug("OnAfterRenderAsync");

            if (!IsInitialized)
                return;

            await base.OnAfterRenderAsync(firstRender);
            await Interop.InitializeAsync(this);

            if (Markers.Count > 0)
                IsZoomed = true;

            if (selected != null)
            {
                await Interop.CenterAtAsync(selected.Latitude, selected.Longitude);
                selected = null;
            }
        }

        public void Dispose()
        {
            Log.Debug("Dispose");
        }

        internal void MoveMarker(int? index, double latitude, double longitude)
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
                MarkersChanged?.Invoke();
            }
        }

        protected async Task SearchLocationAsync()
        {
            SearchModal.Show();
            SearchResults.Clear();
            if (!String.IsNullOrEmpty(SearchQuery))
            {
                var results = await Service.GetGeoLocateListAsync(SearchQuery);
                SearchResults.AddRange(results);

                Log.Debug($"Search, results: {SearchResults.Count}.");

                StateHasChanged();
            }
        }

        protected ValueTask SearchResultSelectedAsync(MapSearchModel selected)
        {
            this.selected = selected;
            SearchModal.Hide();

            return ValueTask.CompletedTask;
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
