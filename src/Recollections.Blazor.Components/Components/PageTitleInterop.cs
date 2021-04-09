using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class PageTitleInterop
    {
        private readonly IJSRuntime jsRuntime;

        public PageTitleInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        internal ValueTask SetAsync(string title)
            => jsRuntime.InvokeVoidAsync("Recollections.SetTitle", title);


    }
}
