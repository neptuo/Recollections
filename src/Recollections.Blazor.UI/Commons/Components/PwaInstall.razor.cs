using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Components
{
    public partial class PwaInstall : ComponentBase, IDisposable
    {
        [Inject]
        internal PwaInstallInterop Interop { get; set; }

        [Inject]
        internal TooltipInterop Tooltip { get; set; }

        [Inject]
        internal ILog<PwaInstall> Log { get; set; }

        protected ElementReference Button { get; set; }
        protected bool IsInstallable { get; set; }

        protected override void OnInitialized()
        {
            Log.Debug("OnInitialized");

            base.OnInitialized();
            Interop.Initialize(this);
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (IsInstallable)
                await Tooltip.InitializeAsync(Button);
        }

        public void MakeInstallable()
        {
            Log.Debug("Installable=True");

            IsInstallable = true;
            StateHasChanged();
        }

        protected async Task InstallAsync()
        {
            await Interop.InstallAsync();
            IsInstallable = false;
        }

        public void Dispose()
        {
            Interop.Remove(this);
        }
    }
}
