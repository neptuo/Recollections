using Microsoft.JSInterop;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public class InlineMarkdownEditInterop
    {
        private readonly IJSRuntime jsRuntime;

        public InlineMarkdownEditInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public Task InitializeAsync(string textAreaId) 
            => jsRuntime.InvokeAsync<object>("InlineMarkdownEdit.Initialize", textAreaId);

        public Task DestroyAsync(string textAreaId) 
            => jsRuntime.InvokeAsync<object>("InlineMarkdownEdit.Destroy", textAreaId);

        internal Task<string> SetValueAsync(string textAreaId, string value)
            => jsRuntime.InvokeAsync<string>("InlineMarkdownEdit.SetValue", textAreaId, value);

        internal Task<string> GetValueAsync(string textAreaId)
            => jsRuntime.InvokeAsync<string>("InlineMarkdownEdit.GetValue", textAreaId);
    }
}
