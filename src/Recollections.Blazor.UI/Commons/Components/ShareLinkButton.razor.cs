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

        protected Toast Toast { get; set; }

        protected void OnClick()
        {
            Interop.CopyToClipboard(Navigator.GetCurrentUrl());
            Toast.Show("URL copied to the clipboard");
        }
    }
}
