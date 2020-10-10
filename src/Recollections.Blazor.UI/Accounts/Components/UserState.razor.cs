using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Components
{
    public partial class UserState
    {
        private TaskCompletionSource<string> authenticationSource = new TaskCompletionSource<string>();

        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Interop Interop { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        public event Action UserChanged;
        public event Action UserInfoChanged;

        public string BearerToken { get; private set; }
        public string UserId { get; private set; }
        public string UserName { get; private set; }
        public bool IsAuthenticated => BearerToken != null;

        private void SetAuthorization(string bearerToken, bool isPersistent)
        {
            BearerToken = bearerToken;
            Api.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            if (isPersistent)
                Interop.SaveToken(bearerToken);

            UserChanged?.Invoke();
            authenticationSource.SetResult(bearerToken);
        }

        private void ClearAuthorization()
        {
            if (BearerToken != null)
            {
                BearerToken = null;
                UserName = null;
                Api.Authorization = null;
                Interop.SaveToken(null);

                UserChanged?.Invoke();
                UserInfoChanged?.Invoke();

                authenticationSource = new TaskCompletionSource<string>();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            if (BearerToken == null)
            {
                string bearerToken = await Interop.LoadTokenAsync();
                if (!string.IsNullOrEmpty(bearerToken))
                {
                    SetAuthorization(bearerToken, false);
                }
                else
                {
                    NavigateToLogin();
                    return;
                }
            }

            await LoadUserInfoAsync();
        }

        private async Task<bool> LoadUserInfoAsync()
        {
            try
            {
                UserInfoResponse response = await Api.GetInfoAsync();
                UserId = response.UserId;
                UserName = response.UserName;
                UserInfoChanged?.Invoke();
            }
            catch (UnauthorizedAccessException)
            {
                ClearAuthorization();
                NavigateToLogin();
                return false;
            }

            return true;
        }

        private void NavigateToLogin() => Navigator.OpenLogin();

        public async Task<bool> LoginAsync(string username, string password, bool isPersistent = false)
        {
            LoginResponse response = await Api.LoginAsync(new LoginRequest(username, password));
            if (response.BearerToken != null)
            {
                SetAuthorization(response.BearerToken, isPersistent);
                await LoadUserInfoAsync();

                Navigator.OpenTimeline();

                return true;
            }

            return false;
        }

        public Task LogoutAsync()
        {
            ClearAuthorization();
            Navigator.OpenLogin();
            return Task.FromResult(true);
        }

        public Task EnsureAuthenticatedAsync() => authenticationSource.Task;
    }
}
