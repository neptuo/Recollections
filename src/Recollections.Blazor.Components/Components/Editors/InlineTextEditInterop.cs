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

        protected InlineTextEditModel Model { get; set; }

        public InlineTextEditInterop(IJSRuntime jsRuntime, ILog<InlineTextEditInterop> log)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            Ensure.NotNull(log, "log");
            this.jsRuntime = jsRuntime;
            this.log = log;
        }

        public ValueTask InitializeAsync(InlineTextEditModel model)
        {
            Model = model;
            return jsRuntime.InvokeVoidAsync("InlineTextEdit.Initialize", DotNetObjectReference.Create(this), model.Input);
        }

        [JSInvokable("TextEdit.OnCancel")]
        public void OnCancel() 
            => Model.OnCancel();
    }
}
