using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class PermissionView
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public PermissionRequest Request { get; set; }

        [CascadingParameter]
        public PermissionContainerState State { get; set; }

        protected bool IsVisible()
        {
            switch (Request)
            {
                case PermissionRequest.Owner:
                    return State.IsEditable;
                case PermissionRequest.Write:
                    return State.IsEditable;
                case PermissionRequest.Read:
                    return State.IsReadable;
                default:
                    throw Ensure.Exception.NotSupported(Request);
            }
        }
    }
}
