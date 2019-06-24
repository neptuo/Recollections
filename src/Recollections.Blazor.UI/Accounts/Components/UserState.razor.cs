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
    public class UserStateModel : ComponentBase
    {
        private readonly TaskCompletionSource<string> authenticationSource = new TaskCompletionSource<string>();

        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected IUriHelper Uri { get; set; }

        [Inject]
        protected Interop Interop { get; set; }

        [Parameter]
        protected RenderFragment ChildContent { get; set; }

        public event Action UserChanged;
        public event Action UserInfoChanged;

        public string BearerToken { get; private set; }
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
            }
        }

        protected override async Task OnInitAsync()
        {
            Console.WriteLine("UserStateModel.Init");

            if (BearerToken == null)
            {
                Console.WriteLine("UserStateModel.TokenLoad");
                string bearerToken = await Interop.LoadTokenAsync();
                Console.WriteLine($"UserStateModel.TokenLoaded '{bearerToken}'.");
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

                UserName = response.username;
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

        private void NavigateToLogin() => Uri.NavigateTo("/login");

        public async Task<bool> LoginAsync(string username, string password, bool isPersistent = false)
        {
            LoginResponse response = await Api.LoginAsync(new LoginRequest(username, password));
            if (response.BearerToken != null)
            {
                SetAuthorization(response.BearerToken, isPersistent);
                await LoadUserInfoAsync();

                Uri.NavigateTo("/");

                return true;
            }

            return false;
        }

        public Task LogoutAsync()
        {
            ClearAuthorization();
            Uri.NavigateTo("/login");
            return Task.FromResult(true);
        }

        public Task EnsureAuthenticatedAsync() => authenticationSource.Task;
    }
}
