using CommonMark;
using Microsoft.AspNetCore.Components;
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

        public async override Task SetParametersAsync(ParameterCollection parameters)
        {
            await base.SetParametersAsync(parameters);

            markdownValue = null;
        }

        protected override void OnValueChanged()
        {
            base.OnValueChanged();
            markdownValue = null;
        }

        private string TransformValue()
        {
            if (String.IsNullOrEmpty(Value))
                return Value;

            string html = CommonMarkConverter.Convert(Value);
            return html;
        }
    }
}
