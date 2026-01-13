using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
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
        public string DialogTitle { get; set; }
        
        [Parameter]
        public string Text { get; set; }
        
        [Parameter]
        public bool IsPlaceHolder { get; set; }

        [Parameter]
        public bool IsEnabled { get; set; } = true;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected Offcanvas Modal { get; set; }
    }
}
