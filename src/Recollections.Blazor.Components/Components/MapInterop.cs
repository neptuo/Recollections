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
using System.Text.Json;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class MapInterop(IJSRuntime js, NavigationManager navigationManager, ILog<MapInterop> log)
    {
        private IJSObjectReference module;
        private Map editor;

        public async Task InitializeAsync(Map editor)
        {
            this.editor = editor;

            module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/Map.js");

            await module.InvokeVoidAsync(
                "ensureApi"
            );

            MapPosition position = null;
            if (!string.IsNullOrEmpty(navigationManager.HistoryEntryState))
            {
                log.Debug($"Restoring map position from history state '{navigationManager.HistoryEntryState}'");
                position = JsonSerializer.Deserialize<MapPosition>(navigationManager.HistoryEntryState);
            }

            await module.InvokeVoidAsync(
                "initialize",
                editor.Container,
                DotNetObjectReference.Create(this),
                editor.Markers,
                editor.IsZoomed || position != null,
                editor.IsEditable
            );

            if (position != null)
            {
                log.Debug($"Centering map at lat={position.Latitude}, lon={position.Longitude}, zoom={position.Zoom}");
                await CenterAtAsync(position.Latitude, position.Longitude, position.Zoom);
            }
        }

        [JSInvokable("MapInterop.MarkerMoved")]
        public void MarkerMoved(int? index, double latitude, double longitude)
            => editor.MoveMarker(index, latitude, longitude);

        [JSInvokable("MapInterop.MarkerSelected")]
        public void MarkerSelected(int index) => editor.MarkerSelected?.Invoke(index);

        [JSInvokable("MapInterop.MoveEnd")]
        public void MoveEnd(double latitude, double longitude, int zoom)
        {
            var userState = JsonSerializer.Serialize(new MapPosition(latitude, longitude, zoom));
            if (navigationManager.HistoryEntryState == userState)
            {
                log.Debug("Map position unchanged in history state.");
                return;
            }

            log.Debug($"Replacing history entry with new map position '{userState}'");
            navigationManager.NavigateTo(
                navigationManager.Uri, 
                new NavigationOptions()
                {
                    ReplaceHistoryEntry = true, 
                    HistoryEntryState = userState
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
    }

    public record MapPosition(double Latitude, double Longitude, int Zoom);
}
