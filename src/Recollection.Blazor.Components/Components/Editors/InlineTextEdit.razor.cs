using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Components.Editors
{
    public class InlineTextEditModel : ComponentBase
    {
        [Parameter]
        protected string Value { get; set; }

        [Parameter]
        protected Action<string> ValueChanged { get; set; }

        [Parameter]
        protected string PlaceHolder { get; set; } 

        protected bool IsEditMode { get; set; }

        protected void OnSaveValue()
        { 
            IsEditMode = false;
            ValueChanged?.Invoke(Value);
        }
    }
}
