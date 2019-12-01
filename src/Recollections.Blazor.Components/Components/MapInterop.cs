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
        private MapModel model;

        public MapInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public ValueTask InitializeAsync(MapModel model)
        {
            this.model = model;
            return jsRuntime.InvokeVoidAsync("Map.Initialize", model.Container, DotNetObjectReference.Create(this), model.Markers, model.IsZoomed, model.IsResizable);
        }

        [JSInvokable("Map.MarkerMoved")]
        public void MarkerMoved(int? index, double latitude, double longitude, double? altitude) 
            => model.MoveMarker(index, latitude, longitude, altitude);

        [JSInvokable("Map.MarkerSelected")]
        public void MarkerSelected(int index) => model.MarkerSelected?.Invoke(index);
    }
}
