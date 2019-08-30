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
            return jsRuntime.InvokeAsync<object>("Map.Initialize", model.Container, DotNetObjectRef.Create(this), model.Zoom);
        }

        public Task SetMarkers(ICollection<LocationModel> markers)
            => jsRuntime.InvokeAsync<object>("Map.SetMarkers", model.Container, markers);
    }
}
