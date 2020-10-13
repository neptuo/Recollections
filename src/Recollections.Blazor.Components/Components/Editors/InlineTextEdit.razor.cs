using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public partial class InlineTextEdit
    {
        [Inject]
        protected InlineTextEditInterop Interop { get; set; }

        [Inject]
        protected ILog<InlineTextEdit> Log { get; set; }

        public ElementReference Input { get; protected set; }

        private bool isEditSwitched = false;

        protected async override Task OnEditAsync()
        {
            await base.OnEditAsync();
            isEditSwitched = true;
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (isEditSwitched)
            {
                await Interop.InitializeAsync(this);
                isEditSwitched = false;
            }
        }

        internal async void OnCancel()
        {
            await OnResetAsync();
            StateHasChanged();
            Log.Debug($"Cancel completed, IsEditMode: {IsEditMode}");
        }
    }
}
