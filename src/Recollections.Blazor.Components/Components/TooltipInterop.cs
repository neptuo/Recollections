using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class TooltipInterop
    {
        private readonly IJSRuntime jsRuntime;

        public TooltipInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public ValueTask InitializeAsync(ElementReference element)
            => jsRuntime.InvokeVoidAsync("Bootstrap.Tooltip.Init", element);

        public ValueTask ShowAsync(ElementReference element)
            => jsRuntime.InvokeVoidAsync("Bootstrap.Tooltip.Show", element);

        public ValueTask HideAsync(ElementReference element)
            => jsRuntime.InvokeVoidAsync("Bootstrap.Tooltip.Hide", element);

        public ValueTask DisposeAsync(ElementReference element)
            => jsRuntime.InvokeVoidAsync("Bootstrap.Tooltip.Dispose", element);
    }
}
