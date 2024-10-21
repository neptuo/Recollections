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
    public class ElementReferenceInterop(IJSRuntime jsRuntime)
    {
        public ValueTask BlurAsync(ElementReference elementRef) 
            => jsRuntime.InvokeVoidAsync("ElementReference.Blur", elementRef);

        public ValueTask ScrollIntoViewAsync(ElementReference elementRef)
            => jsRuntime.InvokeVoidAsync("ElementReference.ScrollIntoView", elementRef);

        public ValueTask<string> GetValueAsync(ElementReference elementRef)
            => jsRuntime.InvokeAsync<string>("ElementReference.GetValue", elementRef);
    }
}
