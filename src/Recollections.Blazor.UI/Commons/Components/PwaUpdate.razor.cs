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
    public partial class PwaUpdate : ComponentBase, IDisposable, PwaInstallInterop.IComponent
    {
        [Inject]
        internal PwaInstallInterop Interop { get; set; }

        [Inject]
        internal ILog<PwaUpdate> Log { get; set; }

        [Inject]
        internal AppUpdateState AppUpdateState { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected bool IsUpdateable { get; set; }
        protected string LastSeenVersion { get; private set; }

        protected override void OnInitialized()
        {
            Log.Debug("OnInitialized");

            base.OnInitialized();
            Interop.Initialize(this);
        }

        public void MakeInstallable()
        { }

        public void MakeUpdateable()
        {
            Log.Debug("Updateable=True");

            _ = InvokeAsync(LoadLastSeenVersionAsync);
        }

        private async Task LoadLastSeenVersionAsync()
        {
            LastSeenVersion = await AppUpdateState.GetLastSeenClientVersionAsync();
            IsUpdateable = true;
            StateHasChanged();
        }

        protected async Task UpdateAsync()
        {
            await AppUpdateState.RememberClientVersionAsync();
            await Interop.UpdateAsync();
        }

        public void Dispose()
        {
            Interop.Remove(this);
        }
    }
}
