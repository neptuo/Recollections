using Microsoft.AspNetCore.Components;
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
    public class ElementReferenceInterop
    {
        private readonly IJSRuntime jsRuntime;

        public ElementReferenceInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public ValueTask BlurAsync(ElementReference elementRef) 
            => jsRuntime.InvokeVoidAsync("ElementReference.Blur", elementRef);

        internal ValueTask ScrollIntoViewAsync(ElementReference elementRef)
            => jsRuntime.InvokeVoidAsync("ElementReference.ScrollIntoView", elementRef);
    }
}
