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

        public async Task InitializedAsync(Gallery component, List<GalleryModel> models)
        {
            Ensure.NotNull(component, "component");
            this.component = component;

            module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/Gallery.js");

            await module.InvokeVoidAsync("initialize", DotNetObjectReference.Create(this), models);
        }

        public async Task OpenAsync(int index)
            => await module.InvokeVoidAsync("open", index);

        public async Task CloseAsync()
            => await module.InvokeVoidAsync("close");
        
        public async Task<bool> IsOpenAsync()
            => await module.InvokeAsync<bool>("isOpen");

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
    }
}
