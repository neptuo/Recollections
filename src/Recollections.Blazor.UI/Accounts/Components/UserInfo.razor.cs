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
    public class UserInfoModel : ComponentBase, IDisposable
    {
        [Inject]
        protected ILog<UserInfoModel> Log { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        protected override void OnInit()
        {
            base.OnInit();

            UserState.UserInfoChanged += OnUserInfoChanged;
        }

        public void Dispose()
        {
            UserState.UserInfoChanged -= OnUserInfoChanged;
        }

        private void OnUserInfoChanged()
        {
            Log.Debug("Raised OnUserInfoChanged.");
            StateHasChanged();
        }

        protected Task LogoutAsync() => UserState.LogoutAsync();
    }
}
