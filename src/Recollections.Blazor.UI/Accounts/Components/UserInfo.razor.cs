using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Components
{
    public partial class UserInfo : IDisposable
    {
        [Inject]
        protected ILog<UserInfo> Log { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            UserState.UserInfoChanged += OnUserInfoChanged;
        }

        public void Dispose()
        {
            UserState.UserInfoChanged -= OnUserInfoChanged;
        }

        private void OnUserInfoChanged()
        {
            Log.Debug($"Raised OnUserInfoChanged, UserName: '{UserState.UserName}'.");
            StateHasChanged();
        }

        protected Task LogoutAsync() => UserState.LogoutAsync();
    }
}
