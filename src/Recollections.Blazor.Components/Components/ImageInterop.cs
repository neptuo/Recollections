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
    public class ImageInterop(IJSRuntime jsRuntime)
    {
        private ValueTask SetInternalAsync(object element, Stream stream)
            => jsRuntime.InvokeVoidAsync("ImageSource.Set", element, new DotNetStreamReference(stream));

        public ValueTask SetAsync(ElementReference element, Stream stream)
            => SetInternalAsync(element, stream);

        public ValueTask SetAsync(IJSObjectReference element, Stream stream)
            => SetInternalAsync(element, stream);
    }
}
