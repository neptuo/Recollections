using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Neptuo;
using Neptuo.Recollections.Entries;
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

        public Task InitializeAsync(MapModel model)
        {
            this.model = model;
            return jsRuntime.InvokeAsync<object>("Map.Initialize", model.Container, DotNetObjectRef.Create(this), model.Zoom, model.IsEditable, model.Markers);
        }

        [JSInvokable]
        public void MarkerMoved(int? index, double latitude, double longitude, double? altitude) 
            => model.MoveMarker(index, latitude, longitude, altitude);
    }
}
