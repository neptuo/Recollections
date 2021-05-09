using Microsoft.JSInterop;
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

            await module.InvokeVoidAsync("ensureApi");

            await module.InvokeVoidAsync(
                "initialize",
                editor.Container,
                DotNetObjectReference.Create(this),
                editor.Markers,
                editor.IsZoomed,
                editor.IsResizable,
                editor.IsEditable
            );
        }

        [JSInvokable("MapInterop.MarkerMoved")]
        public void MarkerMoved(int? index, double latitude, double longitude, double? altitude)
            => editor.MoveMarker(index, latitude, longitude, altitude);

        [JSInvokable("MapInterop.MarkerSelected")]
        public void MarkerSelected(int index) => editor.MarkerSelected?.Invoke(index);

        private TaskCompletionSource<IEnumerable<MapSearchModel>> searchCompletion;

        public Task<IEnumerable<MapSearchModel>> SearchAsync(string searchQuery)
        {
            searchCompletion = new TaskCompletionSource<IEnumerable<MapSearchModel>>();
            _ = module.InvokeVoidAsync("search", editor.Container, searchQuery);

            return searchCompletion.Task;
        }

        [JSInvokable("MapInterop.SearchCompleted")]
        public void SearchCompleted(IEnumerable<MapSearchModel> results)
        {
            searchCompletion.TrySetResult(results);
            searchCompletion = null;
        }

        public async Task CenterAtAsync(double latitude, double longitude)
            => await module.InvokeVoidAsync("centerAt", editor.Container, latitude, longitude);
    }
}
