using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
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
        protected TooltipInterop TooltipInterop { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        protected ElementReference MeButton { get; set; }
        protected ElementReference LogoutButton { get; set; }

        protected async override Task OnInitializedAsync()
        {
            UserState.UserInfoChanged += OnUserInfoChanged;

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (UserState.IsAuthenticated)
            {
                await TooltipInterop.InitializeAsync(MeButton);
                await TooltipInterop.InitializeAsync(LogoutButton);
            }
        }

        public async ValueTask DisposeAsync()
        {
            UserState.UserInfoChanged -= OnUserInfoChanged;

            await TooltipInterop.DisposeAsync(MeButton);
            await TooltipInterop.DisposeAsync(LogoutButton);
        }

        private void OnUserInfoChanged()
        {
            Log.Debug($"Raised OnUserInfoChanged, UserName: '{UserState.UserName}'.");
            StateHasChanged();
        }

        protected Task LogoutAsync() => UserState.LogoutAsync();
    }
}
