using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Components
{
    public partial class ShareLinkButton
    {
        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected WindowInterop Interop { get; set; }

        [Parameter]
        public ButtonLayout Layout { get; set; }

        protected Modal Modal { get; set; }
        protected bool IsCopiedToClipboard { get; set; }

        protected void OnShow()
        {
            IsCopiedToClipboard = false;
            Modal.Show();
        }
    }
}
