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
    public class GalleryInterop : IAsyncDisposable
    {
        private readonly IJSRuntime js;
        private IJSObjectReference module;
        private DotNetObjectReference<GalleryInterop> selfReference;
        private Gallery component;

        public GalleryInterop(IJSRuntime js)
        {
            Ensure.NotNull(js, "js");
            this.js = js;
        }

        public async Task InitializedAsync(Gallery component, List<GalleryModel> models)
        {
            Ensure.NotNull(component, "component");
            this.component = component;

            module ??= await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/Gallery.js");
            selfReference ??= DotNetObjectReference.Create(this);

            await module.InvokeVoidAsync("initialize", selfReference, models);
        }

        public async Task OpenAsync(int index)
        {
            if (module != null)
                await module.InvokeVoidAsync("open", index);
        }

        public async Task CloseAsync()
        {
            if (module != null)
                await module.InvokeVoidAsync("close");
        }

        public async Task<bool> IsOpenAsync()
        {
            if (module == null)
                return false;

            return await module.InvokeAsync<bool>("isOpen");
        }

        [JSInvokable]
        public async Task<DotNetStreamReference> GetImageDataAsync(int index, string type)
        {
            if (component.DataGetter == null)
                return null;

            var stream = await component.DataGetter(index, type);
            return new DotNetStreamReference(stream);
        }

        [JSInvokable]
        public Task OpenInfoAsync(int index)
            => component.OnOpenInfo.InvokeAsync(index);

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();

            if (module != null)
            {
                await module.DisposeAsync();
                module = null;
            }

            selfReference?.Dispose();
            selfReference = null;
            component = null;
        }
    }
}
