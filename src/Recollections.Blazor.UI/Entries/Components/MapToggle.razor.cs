using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class MapToggleModel : ComponentBase
    {
        [Parameter]
        protected string Text { get; set; }

        [Parameter]
        protected Func<bool, string> ToggleChanged { get; set; }

        [Parameter]
        protected RenderFragment ChildContent { get; set; }

        protected bool IsVisible { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            UpdateText();
        }

        protected void OnToggle()
        {
            IsVisible = !IsVisible;
            UpdateText();
        }

        private void UpdateText()
        {
            if (ToggleChanged != null)
            {
                string text = ToggleChanged(IsVisible);
                if (!String.IsNullOrEmpty(text))
                    Text = text;
            }
        }
    }
}
