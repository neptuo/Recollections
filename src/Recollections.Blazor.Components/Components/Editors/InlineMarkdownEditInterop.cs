using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Neptuo;
using Neptuo.Logging;
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
        private readonly ILog<InlineMarkdownEditInterop> log;

        public InlineMarkdownEditModel Model { get; set; }

        public InlineMarkdownEditInterop(IJSRuntime jsRuntime, ILog<InlineMarkdownEditInterop> log)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            Ensure.NotNull(log, "log");
            this.jsRuntime = jsRuntime;
            this.log = log;
        }

        public ValueTask InitializeAsync(InlineMarkdownEditModel model, string value = null)
        {
            Model = model;
            return jsRuntime.InvokeVoidAsync("InlineMarkdownEdit.Initialize", DotNetObjectReference.Create(this), model.TextArea, value);
        }

        internal ValueTask<string> SetValueAsync(ElementReference textArea, string value)
            => jsRuntime.InvokeAsync<string>("InlineMarkdownEdit.SetValue", textArea, value);

        internal ValueTask<string> GetValueAsync(ElementReference textArea)
            => jsRuntime.InvokeAsync<string>("InlineMarkdownEdit.GetValue", textArea);

        [JSInvokable("Markdown.OnSave")]
        public void OnSave(string value) 
            => Model.OnSave(value);

        [JSInvokable("Markdown.OnCancel")]
        public void OnCancel() 
            => Model.OnCancel();
    }
}
