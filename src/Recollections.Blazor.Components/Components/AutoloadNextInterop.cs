using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class AutoloadNextInterop
    {
        private readonly IJSRuntime js;
        private AutoloadNext component;
        private IJSObjectReference module;

        public AutoloadNextInterop(IJSRuntime js)
        {
            Ensure.NotNull(js, "js");
            this.js = js;
        }

        public async Task InitializedAsync(AutoloadNext component)
        {
            Ensure.NotNull(component, "component");
            this.component = component;

            module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/AutoloadNext.js");

            await module.InvokeVoidAsync("observe", component.Container, DotNetObjectReference.Create(this));
        }

        [JSInvokable("intersected")]
        public void Intersected() 
            => _ = component.Intersected.InvokeAsync();
    }
}
