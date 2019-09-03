using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages
{
    public class MeModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected ILog<MeModel> Log { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        public string UserName { get; set; }
        public DateTime Created { get; set; }

        public ChangePasswordViewModel ChangePassword { get; private set; }

        protected async override Task OnInitAsync()
        {
            ChangePassword = new ChangePasswordViewModel(Api);

            await base.OnInitAsync();
            await UserState.EnsureAuthenticatedAsync();

            Log.Debug("Me.GetDetail");
            UserDetailResponse response = await Api.GetDetailAsync();
            UserName = response.UserName;
            Created = response.Created;
        }
    }

    public class ChangePasswordViewModel
    {
        private readonly Api api;

        public ChangePasswordViewModel(Api api)
        {
            Ensure.NotNull(api, "api");
            this.api = api;
        }

        public List<string> ErrorMessages { get; } = new List<string>();
        public string Current { get; set; }
        public string New { get; set; }
        public string ConfirmNew { get; set; }

        public async Task ExecuteAsync()
        {
            if (Validate())
            {
                ChangePasswordResponse response = await api.ChangePasswordAsync(new ChangePasswordRequest(Current, New));
                if (response.IsSuccess)
                {
                    Current = null;
                    New = null;
                    ConfirmNew = null;
                }
                else
                {
                    ErrorMessages.AddRange(response.ErrorMessages);
                }
            }
        }

        private bool Validate()
        {
            ErrorMessages.Clear();

            if (String.IsNullOrEmpty(Current))
                ErrorMessages.Add("Missing current password.");

            if (String.IsNullOrEmpty(New))
                ErrorMessages.Add("Missing new password.");

            if (String.IsNullOrEmpty(ConfirmNew))
                ErrorMessages.Add("Missing new password.");

            if (New != ConfirmNew)
                ErrorMessages.Add("New password and its confirmation must match.");

            return ErrorMessages.Count == 0;
        }
    }
}
