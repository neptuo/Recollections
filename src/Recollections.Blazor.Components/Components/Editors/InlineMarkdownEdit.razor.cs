using CommonMark;
using Microsoft.AspNetCore.Components;
using Neptuo.Identifiers;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public class InlineMarkdownEditModel : InlineEditModel<string>
    {
        [Inject]
        protected InlineMarkdownEditInterop Interop { get; set; }

        [Inject]
        protected MarkdownConverter MarkdownConverter { get; set; }

        [Inject]
        protected ILog<InlineMarkdownEditModel> Log { get; set; }

        public ElementReference TextArea { get; protected set; }

        private MarkupString? markdownValue;

        protected MarkupString MarkdownValue
        {
            get
            {
                if (markdownValue == null)
                    markdownValue = new MarkupString(TransformValue());

                return markdownValue.Value;
            }
        }

        public async override Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);
            markdownValue = null;
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (IsEditMode)
                await Interop.InitializeAsync(this, Value);
        }

        protected async override Task OnValueChangedAsync()
        {
            Value = await Interop.GetValueAsync(TextArea);
            markdownValue = null;

            await base.OnValueChangedAsync();
        }

        internal async void OnSave(string value)
        {
            await OnSaveValueAsync();
            StateHasChanged();
            Log.Debug($"Save completed, IsEditMode: {IsEditMode}");
        }

        internal async void OnCancel()
        {
            await OnResetAsync();
            StateHasChanged();
            Log.Debug($"Cancel completed, IsEditMode: {IsEditMode}");
        }

        private string TransformValue()
        {
            if (String.IsNullOrEmpty(Value))
                return Value;

            string html = MarkdownConverter.Convert(Value);
            return html;
        }
    }
}
