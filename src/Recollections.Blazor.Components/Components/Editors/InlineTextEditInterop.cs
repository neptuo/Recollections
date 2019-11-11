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
        private static ILog<InlineTextEditInterop> log;
        private static Dictionary<string, InlineTextEditModel> models = new Dictionary<string, InlineTextEditModel>();

        public InlineTextEditInterop(IJSRuntime jsRuntime, ILog<InlineTextEditInterop> log)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            Ensure.NotNull(log, "log");
            this.jsRuntime = jsRuntime;
            InlineTextEditInterop.log = log;
        }

        public ValueTask InitializeAsync(InlineTextEditModel model)
        {
            models[model.InputId] = model;
            return jsRuntime.InvokeVoidAsync("InlineTextEdit.Initialize", model.InputId);
        }

        [JSInvokable]
        public static void InlineTextEdit_OnCancel(string inputId)
        {
            log.Debug($"InlineTextEdit_OnCancel, InputId: {inputId}");
            if (models.TryGetValue(inputId, out var model))
            {
                log.Debug("Model found");
                model.OnCancel();
            }
        }
    }
}
