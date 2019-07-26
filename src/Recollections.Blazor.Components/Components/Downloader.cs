using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class Downloader
    {
        private readonly IJSRuntime jsRuntime;

        public Downloader(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public Task FromUrlAsync(string name, string url) 
            => jsRuntime.InvokeAsync<object>("Downloader.FromUrlAsync", name, url);
    }
}
