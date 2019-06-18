using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public class InlineEditModel<T> : ComponentBase
    {
        [Parameter]
        protected T Value { get; set; }

        [Parameter]
        protected Action<T> ValueChanged { get; set; }

        [Parameter]
        protected string PlaceHolder { get; set; }

        protected bool IsEditMode { get; set; }

        protected void OnSaveValue()
        {
            IsEditMode = false;
            ValueChanged?.Invoke(Value);
        }

        protected string GetModeCssClass() 
            => IsEditMode ? String.Empty : "inline-editor-viewmode";
    }
}
