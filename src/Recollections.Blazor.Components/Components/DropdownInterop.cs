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
    public class DropdownInterop
    {
        private readonly IJSRuntime js;

        public DropdownInterop(IJSRuntime js)
        {
            Ensure.NotNull(js, "js");
            this.js = js;
        }

        public ValueTask InitializeAsync(ElementReference element)
            => js.InvokeVoidAsync("Bootstrap.Dropdown.Init", element);

        public ValueTask ShowAsync(ElementReference element)
            => js.InvokeVoidAsync("Bootstrap.Dropdown.Show", element);

        public ValueTask HideAsync(ElementReference element)
            => js.InvokeVoidAsync("Bootstrap.Dropdown.Hide", element);

        public ValueTask DisposeAsync(ElementReference element)
            => js.InvokeVoidAsync("Bootstrap.Dropdown.Dispose", element);
    }
}
