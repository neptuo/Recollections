using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection
{
    public class DialogBase : ComponentBase
    {
        private bool isVisible;

        [Parameter]
        protected bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible != value)
                {
                    isVisible = value;
                    IsVisibleChanged?.Invoke(isVisible);
                }
            }
        }

        [Parameter]
        protected Action<bool> IsVisibleChanged { get; set; }
    }
}
