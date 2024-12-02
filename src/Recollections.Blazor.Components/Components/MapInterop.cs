using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class MapInterop
    {
        private readonly IJSRuntime js;
        private IJSObjectReference module;
        private Map editor;

        public MapInterop(IJSRuntime js)
        {
            Ensure.NotNull(js, "js");
            this.js = js;
        }

        public async Task InitializeAsync(Map editor)
        {
            this.editor = editor;

            module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/Map.js");

            await module.InvokeVoidAsync(
                "ensureApi"
            );

            await module.InvokeVoidAsync(
                "initialize",
                editor.Container,
                DotNetObjectReference.Create(this),
                editor.Markers,
                editor.IsZoomed,
                editor.IsEditable
            );
        }

        [JSInvokable("MapInterop.MarkerMoved")]
        public void MarkerMoved(int? index, double latitude, double longitude, double? altitude)
            => editor.MoveMarker(index, latitude, longitude, altitude);

        [JSInvokable("MapInterop.MarkerSelected")]
        public void MarkerSelected(int index) => editor.MarkerSelected?.Invoke(index);

        [JSInvokable("MapInterop.LoadTile")]
        public Task LoadTileAsync(JSObjectReference img, int x, int y, int z)
            => editor.LoadTileAsync(img, x, y, z);

        public async Task CenterAtAsync(double latitude, double longitude)
            => await module.InvokeVoidAsync("centerAt", editor.Container, latitude, longitude);

        public async Task RedrawAsync()
            => await module.InvokeVoidAsync("redraw", editor.Container);
    }
}
