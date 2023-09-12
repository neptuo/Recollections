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
    public partial class PwaInstall : ComponentBase, IAsyncDisposable
    {
        [Inject]
        internal PwaInstallInterop Interop { get; set; }

        [Inject]
        internal TooltipInterop Tooltip { get; set; }

        [Inject]
        internal ILog<PwaInstall> Log { get; set; }

        [Inject]
        internal Navigator Navigator { get; set; }

        [Parameter]
        public EventCallback OnUpdateAvailable { get; set; }

        protected ElementReference Button { get; set; }
        protected bool IsInstallable { get; set; }
        protected bool IsUpdateable { get; set; }

        protected override void OnInitialized()
        {
            Log.Debug("OnInitialized");

            base.OnInitialized();
            Interop.Initialize(this);
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (IsInstallable || IsUpdateable)
                await Tooltip.InitializeAsync(Button);

            if (!IsInstallable && IsUpdateable)
            {
                await OnUpdateAvailable.InvokeAsync();
                await Tooltip.ShowAsync(Button);
            }
        }

        public async void MakeInstallable()
        {
            Log.Debug("Installable=True");

            await Tooltip.DisposeAsync(Button);

            IsInstallable = true;
            StateHasChanged();
        }

        public async void MakeUpdateable()
        {
            Log.Debug("Updateable=True");
            
            await Tooltip.DisposeAsync(Button);

            IsUpdateable = true;
            StateHasChanged();
        }

        protected async Task InstallAsync()
        {
            await Tooltip.HideAsync(Button);
            await Interop.InstallAsync();
            IsInstallable = false;
        }

        protected async Task UpdateAsync()
        {
            await Tooltip.HideAsync(Button);
            await Interop.UpdateAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Interop.Remove(this);

            await Tooltip.DisposeAsync(Button);
        }
    }
}
