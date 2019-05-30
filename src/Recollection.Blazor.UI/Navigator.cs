using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection
{
    public class Navigator
    {
        private readonly IJSRuntime jsRuntime;

        public Navigator(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public Task<bool> AskAsync(string message)
            => jsRuntime.InvokeAsync<bool>("window.confirm", message);
    }
}
