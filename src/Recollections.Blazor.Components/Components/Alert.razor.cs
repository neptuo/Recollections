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

        private AlertMode mode;

        [Parameter]
        public AlertMode Mode
        {
            get => mode;
            set
            {
                if (mode != value)
                {
                    mode = value;
                    UpdateModeCssClass();
                }
            }
        }

        [Parameter]
        public string CssClass { get; set; }

        protected string ModeCssClass { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            UpdateModeCssClass();
        }

        protected void UpdateModeCssClass()
        {
            switch (mode)
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
                    throw Ensure.Exception.NotSupported(mode.ToString());
            }

            if (OnDismiss.HasDelegate)
            {
                ModeCssClass += " alert-dismissible";
            }
        }
    }
}
