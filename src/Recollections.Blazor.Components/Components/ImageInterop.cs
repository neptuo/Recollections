using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class ImageInterop
    {
        private readonly IJSRuntime jsRuntime;

        public ImageInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public ValueTask SetAsync(ElementReference element, Stream stream)
            => jsRuntime.InvokeVoidAsync("ImageSource.Set", element, new DotNetStreamReference(stream));
    }
}
