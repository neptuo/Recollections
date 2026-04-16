using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class MapInterop(IJSRuntime js, NavigationManager navigationManager, ILog<MapInterop> log)
    {
        private IJSObjectReference module;
        private Map editor;
        private DotNetObjectReference<MapInterop> self;

        private int previousMarkersHashCode;
        private int previousMapPositionHashCode;
        private string previousViewMode;

        private int ComputeMarkersHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(editor.Markers ?? []);
            if (editor.Markers != null)
            {
                hashCode.Add(editor.Markers.Count);
                foreach (var marker in editor.Markers)
                    hashCode.Add(marker);
            }

            hashCode.Add(editor.Path);

            return hashCode.ToHashCode();
        }

        public bool ShouldRender()
        {
            // Duplicated logic from InitializeAsync.
            
            if (module == null)
                return true;

            var markersHashCode = ComputeMarkersHashCode();
            var hasMarkersChanged = previousMarkersHashCode != markersHashCode;
            if (hasMarkersChanged)
                return true;
            
            MapPosition position = FindMapPositionFromHistoryEntry();
            if (position != null)
            {
                var mapPositionHashCode = position.GetHashCode();
                if (previousMapPositionHashCode != mapPositionHashCode)
                    return true;
            }
            else
            {
                if (hasMarkersChanged)
                    return true;
            }

            return false;
        }

        public async Task InitializeAsync(Map editor)
        {
            this.editor = editor;

            if (module == null)
            {
                module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/Map.js");
                await module.InvokeVoidAsync("ensureApi");

                if (self == null)
                    self = DotNetObjectReference.Create(this);

                await module.InvokeVoidAsync(
                    "initialize",
                    editor.Container,
                    self,
                    editor.IsEditable
                );
            }

            MapPosition position = FindMapPositionFromHistoryEntry();

            var markersHashCode = ComputeMarkersHashCode();
            var hasMarkersChanged = previousMarkersHashCode != markersHashCode;
            if (hasMarkersChanged)
            {
                log.Debug("Markers changed, updating map markers.");
                previousMarkersHashCode = markersHashCode;

                await module.InvokeVoidAsync(
                    "updateMarkers", 
                    editor.Container, 
                    editor.Markers,
                    editor.Path,
                    editor.IsEditable
                );
            }

            if (position != null)
            {
                var mapPositionHashCode = position.GetHashCode();
                if (previousMapPositionHashCode != mapPositionHashCode)
                {
                    previousMapPositionHashCode = mapPositionHashCode;
                    log.Debug($"Position changed, centering map at lat={position.Latitude}, lon={position.Longitude}, zoom={position.Zoom}");
                    await CenterAtAsync(position.Latitude, position.Longitude, position.Zoom);
                }
            }
            else
            {
                if (hasMarkersChanged)
                {
                    log.Debug("Centering map at markers.");
                    await module.InvokeVoidAsync("centerAtMarkers", editor.Container);
                }
            }
        }

        private MapPosition FindMapPositionFromHistoryEntry()
        {
            MapPosition position = null;
            if (!string.IsNullOrEmpty(navigationManager.HistoryEntryState))
            {
                log.Debug($"Reading map position from history state '{navigationManager.HistoryEntryState}'");
                position = PageHistoryState.Parse(navigationManager.HistoryEntryState).Map;
            }

            return position;
        }

        [JSInvokable("MapInterop.MarkerMoved")]
        public void MarkerMoved(int? index, double latitude, double longitude)
            => editor.MoveMarker(index, latitude, longitude);

        [JSInvokable("MapInterop.MarkerSelected")]
        public async void MarkerSelected(int index) => await editor.MarkerSelected.InvokeAsync(index);

        [JSInvokable("MapInterop.PathSelected")]
        public async Task PathSelected()
            => await editor.SelectPathAsync();

        [JSInvokable("MapInterop.MoveEnd")]
        public void MoveEnd(double latitude, double longitude, int zoom)
        {
            // We don't need another round through OnAfterRenderAsync
            var position = new MapPosition(latitude, longitude, zoom);
            previousMapPositionHashCode = position.GetHashCode();

            var userState = PageHistoryState.Parse(navigationManager.HistoryEntryState);
            if (userState.Map == position)
            {
                log.Debug("Map position unchanged in history state.");
                return;
            }

            userState.Map = position;
            var serializedState = userState.ToJson();

            log.Debug($"Replacing history entry with new map position '{serializedState}'");
            navigationManager.NavigateTo(
                navigationManager.Uri, 
                new NavigationOptions()
                {
                    ReplaceHistoryEntry = true, 
                    HistoryEntryState = serializedState
                }
            );
        }

        [JSInvokable("MapInterop.LoadTile")]
        public Task LoadTileAsync(JSObjectReference img, int x, int y, int z)
            => editor.LoadTileAsync(img, x, y, z);

        public async Task CenterAtAsync(double latitude, double longitude, int? zoom = null)
            => await module.InvokeVoidAsync("centerAt", editor.Container, latitude, longitude, zoom);

        public async Task RedrawAsync()
            => await module.InvokeVoidAsync("redraw", editor.Container);

        public async Task SetViewModeAsync(string mode, string countriesGeoJson)
        {
            if (module == null)
                return;

            previousViewMode = mode;
            await module.InvokeVoidAsync("setViewMode", editor.Container, mode, countriesGeoJson);
        }

        public async Task ShowMarkerPopoverAsync(int markerIndex, ElementReference content)
            => await module.InvokeVoidAsync("showMarkerPopover", editor.Container, markerIndex, content);
    }

    public record MapPosition(double Latitude, double Longitude, int Zoom);
}
