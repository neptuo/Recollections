using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
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
        internal ILog<PwaInstall> Log { get; set; }

        protected bool IsInstallable { get; set; }

        protected override void OnInitialized()
        {
            Log.Debug("OnInitialized");

            base.OnInitialized();
            Interop.Initialize(this);
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
