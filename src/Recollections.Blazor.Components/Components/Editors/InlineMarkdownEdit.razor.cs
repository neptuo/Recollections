using CommonMark;
using Microsoft.AspNetCore.Components;
using Neptuo.Identifiers;
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
        protected IUniqueNameProvider NameProvider { get; set; }

        [Inject]
        protected MarkdownConverter MarkdownConverter { get; set; }

        public string TextAreaId { get; private set; }

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

        protected override void OnInit()
        {
            base.OnInit();
            TextAreaId = NameProvider.Next();
        }

        public async override Task SetParametersAsync(ParameterCollection parameters)
        {
            await base.SetParametersAsync(parameters);
            markdownValue = null;
        }

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();

            if (IsEditMode)
            {
                await Interop.InitializeAsync(TextAreaId);
                await Interop.SetValueAsync(TextAreaId, Value);
            }
            else
            {
                await Interop.DestroyAsync(TextAreaId);
            }
        }

        protected async override Task OnValueChangedAsync()
        {
            Value = await Interop.GetValueAsync(TextAreaId);
            markdownValue = null;

            await base.OnValueChangedAsync();
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
