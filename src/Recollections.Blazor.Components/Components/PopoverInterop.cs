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
    public class PopoverInterop
    {
        private readonly IJSRuntime jsRuntime;

        public PopoverInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public ValueTask ShowAsync(ElementReference element, string title, string body)
            => jsRuntime.InvokeVoidAsync("Bootstrap.Popover.Show", element, title, body);
    }
}
