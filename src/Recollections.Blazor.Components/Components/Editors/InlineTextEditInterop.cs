using Microsoft.JSInterop;
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
        private static Dictionary<string, InlineTextEditModel> models = new Dictionary<string, InlineTextEditModel>();

        public InlineTextEditInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public Task InitializeAsync(InlineTextEditModel model)
        {
            models[model.InputId] = model;
            return jsRuntime.InvokeAsync<object>("InlineTextEdit.Initialize", model.InputId);
        }

        [JSInvokable]
        public static void InlineTextEdit_OnCancel(string inputId)
        {
            Console.WriteLine($"InlineTextEdit_OnCancel, InputId: {inputId}");
            if (models.TryGetValue(inputId, out var model))
            {
                Console.WriteLine("Model found");
                model.OnCancel();
            }
        }
    }
}
