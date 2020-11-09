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
        private TaskCompletionSource<string> initializationSource = new TaskCompletionSource<string>();

        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected TokenStorage TokenStorage { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        public event Action UserChanged;
        public event Action UserInfoChanged;

        public string BearerToken { get; private set; }
        public string UserId { get; private set; }
        public string UserName { get; private set; }
        public bool IsAuthenticated => BearerToken != null;

        private async Task SetAuthorizationAsync(string bearerToken, bool isPersistent, bool isStore = true)
        {
            BearerToken = bearerToken;
            Api.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            if (isStore)
                await TokenStorage.SetAsync(bearerToken, isPersistent);

            UserChanged?.Invoke();
        }

        private async Task ClearAuthorizationAsync()
        {
            if (BearerToken != null)
            {
                BearerToken = null;
                UserId = null;
                UserName = null;
                Api.Authorization = null;
                await TokenStorage.ClearAsync();

                UserChanged?.Invoke();
                UserInfoChanged?.Invoke();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            if (BearerToken == null)
            {
                string bearerToken = await TokenStorage.FindAsync();
                if (!string.IsNullOrEmpty(bearerToken))
                {
                    await SetAuthorizationAsync(bearerToken, false, false);
                    await LoadUserInfoAsync();
                }
            }

            initializationSource.SetResult(null);
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
                await ClearAuthorizationAsync();
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
                await SetAuthorizationAsync(response.BearerToken, isPersistent);
                await LoadUserInfoAsync();

                Navigator.OpenTimeline();
                return true;
            }

            return false;
        }

        public async Task LogoutAsync()
        {
            await ClearAuthorizationAsync();
            Navigator.OpenLogin();
        }

        public Task EnsureInitializedAsync()
            => initializationSource.Task;

        public async Task EnsureAuthenticatedAsync()
        {
            await EnsureInitializedAsync();

            if (!IsAuthenticated)
                NavigateToLogin();
        }
    }
}
