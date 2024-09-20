using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Commons.Layouts;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Components
{
    public partial class UserInfo : IAsyncDisposable
    {
        [Inject]
        protected ILog<UserInfo> Log { get; set; }

        [Inject]
        protected DropdownInterop DropdownInterop { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [CascadingParameter]
        public UserState UserState { get; set; }

        [Parameter]
        public MenuList Menu { get; set; }

        [Parameter]
        public EventCallback OnChangePassword { get; set; }

        protected ElementReference MeButton { get; set; }

        protected async override Task OnInitializedAsync()
        {
            UserState.UserInfoChanged += OnUserInfoChanged;

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (UserState.IsAuthenticated)
                await DropdownInterop.InitializeAsync(MeButton);
        }

        public async ValueTask DisposeAsync()
        {
            UserState.UserInfoChanged -= OnUserInfoChanged;

            await DropdownInterop.DisposeAsync(MeButton);
        }

        private void OnUserInfoChanged()
        {
            Log.Debug($"Raised OnUserInfoChanged, UserName: '{UserState.UserName}'.");
            StateHasChanged();
        }

        protected Task LoginAsync() 
            => UserState.EnsureAuthenticatedAsync();

        protected Task LogoutAsync() 
            => UserState.LogoutAsync();
    }
}
