using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class MapToggle
    {
        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public Func<bool, string> ToggleChanged { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

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
