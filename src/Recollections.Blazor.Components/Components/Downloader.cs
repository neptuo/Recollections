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

        public ValueTask FromUrl(string name, string url) 
            => jsRuntime.InvokeVoidAsync("Downloader.FromUrl", name, url);

        public ValueTask FromStreamAsync(string name, Stream stream, string mimeType) 
            => jsRuntime.InvokeVoidAsync("Downloader.FromStreamAsync", name, new DotNetStreamReference(stream), mimeType);
    }
}
