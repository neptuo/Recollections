using Microsoft.AspNetCore.Components;
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
            Console.WriteLine("Raised OnUserInfoChanged.");
            StateHasChanged();
        }

        protected Task LogoutAsync() => UserState.LogoutAsync();
    }
}
