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

        internal void Show(ElementRef element)
            => jsRuntime.InvokeAsync<object>("Bootstrap.Modal.Show", element);

        internal void Hide(ElementRef element)
            => jsRuntime.InvokeAsync<object>("Bootstrap.Modal.Hide", element);
    }
}
