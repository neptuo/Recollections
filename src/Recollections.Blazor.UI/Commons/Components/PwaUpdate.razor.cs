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

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected bool IsUpdateable { get; set; }

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

            IsUpdateable = true;
            StateHasChanged();
        }

        protected async Task UpdateAsync()
        {
            await Interop.UpdateAsync();
            IsUpdateable = false;
        }

        public void Dispose()
        {
            Interop.Remove(this);
        }
    }
}
