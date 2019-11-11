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
        private static ILog<InlineMarkdownEditInterop> log;
        private static Dictionary<string, InlineMarkdownEditModel> models = new Dictionary<string, InlineMarkdownEditModel>();

        public InlineMarkdownEditInterop(IJSRuntime jsRuntime, ILog<InlineMarkdownEditInterop> log)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            Ensure.NotNull(log, "log");
            this.jsRuntime = jsRuntime;
            InlineMarkdownEditInterop.log = log;
        }

        public ValueTask InitializeAsync(InlineMarkdownEditModel model)
        {
            if (!models.ContainsKey(model.TextAreaId))
                models[model.TextAreaId] = model;


            return jsRuntime.InvokeVoidAsync("InlineMarkdownEdit.Initialize", model.TextAreaId);
        }

        public ValueTask DestroyAsync(string textAreaId)
        {
            models.Remove(textAreaId);
            return jsRuntime.InvokeVoidAsync("InlineMarkdownEdit.Destroy", textAreaId);
        }

        internal ValueTask<string> SetValueAsync(string textAreaId, string value)
            => jsRuntime.InvokeAsync<string>("InlineMarkdownEdit.SetValue", textAreaId, value);

        internal ValueTask<string> GetValueAsync(string textAreaId)
            => jsRuntime.InvokeAsync<string>("InlineMarkdownEdit.GetValue", textAreaId);

        [JSInvokable]
        public static void InlineMarkdownEdit_OnSave(string id, string value)
        {
            log.Debug($"InlineMarkdownEdit_OnSave, TextAreaId: {id}");
            if (models.TryGetValue(id, out InlineMarkdownEditModel model))
            {
                log.Debug("Model found");
                model.OnSave(value);
            }
        }

        [JSInvokable]
        public static void InlineMarkdownEdit_OnCancel(string id)
        {
            log.Debug($"InlineMarkdownEdit_OnCancel, TextAreaId: {id}");
            if (models.TryGetValue(id, out InlineMarkdownEditModel model))
            {
                log.Debug("Model found");
                model.OnCancel();
            }
        }
    }
}
