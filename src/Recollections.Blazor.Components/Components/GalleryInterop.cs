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
    public class GalleryInterop
    {
        private readonly IJSRuntime js;
        private IJSObjectReference module;
        private Gallery component;

        public GalleryInterop(IJSRuntime js)
        {
            Ensure.NotNull(js, "js");
            this.js = js;
        }

        public async Task InitializedAsync(Gallery component, int imageCount)
        {
            Ensure.NotNull(component, "component");
            this.component = component;

            module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/Gallery.js");

            await module.InvokeVoidAsync("initialize", DotNetObjectReference.Create(this), imageCount);
        }

        public async Task OpenAsync()
            => await module.InvokeVoidAsync("open");

        [JSInvokable]
        public Task<string> GetImageDataAsync(int index)
        {
            if (component.DataGetter == null)
                return Task.FromResult<string>(null);

            return component.DataGetter(index);
        }
    }
}
