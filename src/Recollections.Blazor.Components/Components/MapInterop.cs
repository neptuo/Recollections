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
        private readonly IJSRuntime jsRuntime;
        private Map editor;

        public MapInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public ValueTask InitializeAsync(Map editor)
        {
            this.editor = editor;
            return jsRuntime.InvokeVoidAsync("Map.Initialize", editor.Container, DotNetObjectReference.Create(this), editor.Markers, editor.IsZoomed, editor.IsResizable);
        }

        [JSInvokable("Map.MarkerMoved")]
        public void MarkerMoved(int? index, double latitude, double longitude, double? altitude)
            => editor.MoveMarker(index, latitude, longitude, altitude);

        [JSInvokable("Map.MarkerSelected")]
        public void MarkerSelected(int index) => editor.MarkerSelected?.Invoke(index);

        private TaskCompletionSource<IEnumerable<MapSearchModel>> searchCompletion;

        public Task<IEnumerable<MapSearchModel>> SearchAsync(string searchQuery)
        {
            searchCompletion = new TaskCompletionSource<IEnumerable<MapSearchModel>>();
            _ = jsRuntime.InvokeVoidAsync("Map.Search", editor.Container, searchQuery);

            return searchCompletion.Task;
        }

        [JSInvokable("Map.SearchCompleted")]
        public void SearchCompleted(IEnumerable<MapSearchModel> results)
        {
            searchCompletion.TrySetResult(results);
            searchCompletion = null;
        }

        public ValueTask CenterAtAsync(double latitude, double longitude)
            => jsRuntime.InvokeVoidAsync("Map.CenterAt", editor.Container, latitude, longitude);
    }
}
