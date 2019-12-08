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
    public class InlineTextEditInterop
    {
        private readonly IJSRuntime jsRuntime;
        private ILog<InlineTextEditInterop> log;

        protected InlineTextEdit Editor { get; set; }

        public InlineTextEditInterop(IJSRuntime jsRuntime, ILog<InlineTextEditInterop> log)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            Ensure.NotNull(log, "log");
            this.jsRuntime = jsRuntime;
            this.log = log;
        }

        public ValueTask InitializeAsync(InlineTextEdit editor)
        {
            Editor = editor;
            return jsRuntime.InvokeVoidAsync("InlineTextEdit.Initialize", DotNetObjectReference.Create(this), editor.Input);
        }

        [JSInvokable("TextEdit.OnCancel")]
        public void OnCancel() 
            => Editor.OnCancel();
    }
}
