using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class ModalInterop
    {
        private readonly IJSRuntime jsRuntime;

        public ModalInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        internal void Show(ElementReference element)
            => jsRuntime.InvokeVoidAsync("Bootstrap.Modal.Show", element);

        internal void Hide(ElementReference element)
            => jsRuntime.InvokeVoidAsync("Bootstrap.Modal.Hide", element);

        internal void Dispose(ElementReference element)
            => jsRuntime.InvokeVoidAsync("Bootstrap.Modal.Dispose", element);

        internal ValueTask<bool> IsOpenAsync(ElementReference element)
            => jsRuntime.InvokeAsync<bool>("Bootstrap.Modal.IsOpen", element);
    }
}
