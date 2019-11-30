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
    public class DatePickerInterop
    {
        private readonly IJSRuntime jsRuntime;

        public DatePickerInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public ValueTask InitializeAsync(ElementReference input, string format)
            => jsRuntime.InvokeVoidAsync("DatePicker.Initialize", input, format);

        public ValueTask DestroyAsync(ElementReference input)
            => jsRuntime.InvokeVoidAsync("DatePicker.Destroy", input);

        public ValueTask<string> GetValueAsync(ElementReference input)
            => jsRuntime.InvokeAsync<string>("DatePicker.GetValue", input);
    }
}
