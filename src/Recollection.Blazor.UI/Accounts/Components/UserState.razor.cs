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

namespace Neptuo.Recollection.Accounts.Components
{
    public class UserStateModel : ComponentBase
    {
        [Inject]
        protected HttpClient HttpClient { get; set; }

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

        public static string Url(string appRelative) => $"http://localhost:33880/api{appRelative}";

        private void SetAuthorization(string bearerToken, bool isPersistent)
        {
            BearerToken = bearerToken;
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            if (isPersistent)
                Interop.SaveToken(bearerToken);

            UserChanged?.Invoke();
        }

        private void ClearAuthorization()
        {
            if (BearerToken != null)
            {
                BearerToken = null;
                UserName = null;
                HttpClient.DefaultRequestHeaders.Authorization = null;
                Interop.SaveToken(null);

                UserChanged?.Invoke();
                UserInfoChanged?.Invoke();
            }
        }

        protected override async Task OnInitAsync()
        {
            if (BearerToken == null)
            {
                string bearerToken = await Interop.LoadTokenAsync();
                Console.WriteLine($"Loaded token '{bearerToken}'.");
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
            HttpResponseMessage response = await HttpClient.GetAsync(Url("/accounts/info"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ClearAuthorization();
                NavigateToLogin();
                return false;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
            UserInfoResponse responseModel = SimpleJson.SimpleJson.DeserializeObject<UserInfoResponse>(responseContent);

            UserName = responseModel.username;
            Console.WriteLine($"Set username to {UserName}");
            UserInfoChanged?.Invoke();

            return true;
        }

        private void NavigateToLogin() => Uri.NavigateTo("/login");

        public async Task LoginAsync(string username, string password, bool isPersistent = false)
        {
            LoginResponse response = await HttpClient.PostJsonAsync<LoginResponse>(Url("/accounts/login"), new LoginRequest(username, password));
            if (response.BearerToken != null)
            {
                SetAuthorization(response.BearerToken, isPersistent);
                await LoadUserInfoAsync();

                Uri.NavigateTo("/");
            }
        }

        public Task LogoutAsync()
        {
            ClearAuthorization();
            Uri.NavigateTo("/login");
            return Task.FromResult(true);
        }
    }
}
