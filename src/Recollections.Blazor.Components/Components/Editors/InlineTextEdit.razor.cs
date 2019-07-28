﻿using Microsoft.AspNetCore.Components;
using Neptuo.Identifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public class InlineTextEditModel : InlineEditModel<string>
    {
        [Inject]
        protected IUniqueNameProvider NameProvider { get; set; }

        [Inject]
        protected InlineTextEditInterop Interop { get; set; }

        public string InputId { get; private set; }

        protected override void OnInit()
        {
            base.OnInit();
            InputId = NameProvider.Next();
        }

        private bool isEditSwitched = false;

        protected async override Task OnEditAsync()
        {
            await base.OnEditAsync();
            isEditSwitched = true;
        }

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();

            if (isEditSwitched)
            {
                await Interop.InitializeAsync(this);
                isEditSwitched = false;
            }
        }

        internal async Task OnCancelAsync()
        {
            await OnResetAsync();
            StateHasChanged();
        }
    }
}
