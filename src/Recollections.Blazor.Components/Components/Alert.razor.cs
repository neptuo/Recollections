using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class Alert : ComponentBase
    {
        [Parameter]
        public string Title { get; set; }

        [Parameter]
        public string Message { get; set; }

        [Parameter]
        public IEnumerable<string> Messages { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public EventCallback OnDismiss { get; set; }

        [Parameter]
        public AlertMode Mode { get; set; }

        [Parameter]
        public string CssClass { get; set; }

        protected string ModeCssClass { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            UpdateModeCssClass();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            UpdateModeCssClass();
        }

        protected void UpdateModeCssClass()
        {
            switch (Mode)
            {
                case AlertMode.Success:
                    ModeCssClass = "alert-success";
                    break;
                case AlertMode.Info:
                    ModeCssClass = "alert-info";
                    break;
                case AlertMode.Warning:
                    ModeCssClass = "alert-warning";
                    break;
                case AlertMode.Error:
                    ModeCssClass = "alert-danger";
                    break;
                default:
                    throw Ensure.Exception.NotSupported(Mode);
            }

            if (OnDismiss.HasDelegate)
            {
                ModeCssClass += " alert-dismissible";
            }
        }
    }
}
