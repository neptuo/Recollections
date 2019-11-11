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

        public ValueTask InitializeAsync(string inputId, string format)
            => jsRuntime.InvokeVoidAsync("DatePicker.Initialize", inputId, format);

        public ValueTask DestroyAsync(string inputId)
            => jsRuntime.InvokeVoidAsync("DatePicker.Destroy", inputId);

        public ValueTask<string> GetValueAsync(string inputId)
            => jsRuntime.InvokeAsync<string>("DatePicker.GetValue", inputId);
    }
}
