using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public class InlineDateEditInterop
    {
        private readonly IJSRuntime jsRuntime;

        public InlineDateEditInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public Task InitializeAsync(string inputId, string format)
            => jsRuntime.InvokeAsync<object>("InlineDateEdit.Initialize", inputId, format);

        public Task DestroyAsync(string inputId)
            => jsRuntime.InvokeAsync<object>("InlineDateEdit.Destroy", inputId);

        public Task<string> GetValueAsync(string inputId)
            => jsRuntime.InvokeAsync<string>("InlineDateEdit.GetValue", inputId);
    }
}
