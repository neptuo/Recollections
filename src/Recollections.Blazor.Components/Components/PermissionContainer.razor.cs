using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class PermissionContainer
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public PermissionContainerState State { get; set; }
    }
}
