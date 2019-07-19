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
        private static Dictionary<string, InlineMarkdownEditModel> models = new Dictionary<string, InlineMarkdownEditModel>();

        public InlineMarkdownEditInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public Task InitializeAsync(InlineMarkdownEditModel model)
        {
            if (!models.ContainsKey(model.TextAreaId))
                models[model.TextAreaId] = model;


            return jsRuntime.InvokeAsync<object>("InlineMarkdownEdit.Initialize", model.TextAreaId);
        }

        public Task DestroyAsync(string textAreaId)
        {
            models.Remove(textAreaId);
            return jsRuntime.InvokeAsync<object>("InlineMarkdownEdit.Destroy", textAreaId);
        }

        internal Task<string> SetValueAsync(string textAreaId, string value)
            => jsRuntime.InvokeAsync<string>("InlineMarkdownEdit.SetValue", textAreaId, value);

        internal Task<string> GetValueAsync(string textAreaId)
            => jsRuntime.InvokeAsync<string>("InlineMarkdownEdit.GetValue", textAreaId);

        [JSInvokable]
        public static void InlineMarkdownEdit_OnSave(string id, string value)
        {
            Console.WriteLine($"InlineMarkdownEdit_OnSave, TextAreaId: {id}");
            if (models.TryGetValue(id, out InlineMarkdownEditModel model))
            {
                Console.WriteLine("Model found");
                model.OnSave(value);
            }
        }
    }
}
