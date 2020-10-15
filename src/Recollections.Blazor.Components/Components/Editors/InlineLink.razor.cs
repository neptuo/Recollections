using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public partial class InlineLink
    {
        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string IconPrefix { get; set; }

        [Parameter]
        public bool? IsClickable { get; set; }

        [Parameter]
        public EventCallback OnClick { get; set; }

        protected override bool IsEditable => IsClickable ?? base.IsEditable;

        protected async override Task OnEditAsync()
        {
            await base.OnEditAsync();

            if (IsEditMode)
                await OnClick.InvokeAsync();

            await OnResetAsync();
        }
    }
}
