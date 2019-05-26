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

        [Parameter]
        protected RenderFragment ChildContent { get; set; }

        public event Action UserChanged;
        public event Action UserInfoChanged;

        public string BearerToken { get; private set; }
        public string Username { get; private set; }
        public bool IsAuthenticated => BearerToken != null;

        public static string Url(string appRelative) => $"http://localhost:62198/api{appRelative}";

        private void SetAuthorization(string bearerToken)
        {
            BearerToken = bearerToken;
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            UserChanged?.Invoke();
        }

        private void ClearAuthorization()
        {
            if (BearerToken != null)
            {
                BearerToken = null;
                Username = null;
                HttpClient.DefaultRequestHeaders.Authorization = null;
                UserChanged?.Invoke();
                UserInfoChanged?.Invoke();
            }
        }

        protected override async Task OnInitAsync()
        {
            await LoadUserInfoAsync();
        }

        private async Task<bool> LoadUserInfoAsync()
        {
            HttpResponseMessage response = await HttpClient.GetAsync(Url("/accounts/info"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ClearAuthorization();
                Uri.NavigateTo("/login");
                return false;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
            UserInfoResponse responseModel = SimpleJson.SimpleJson.DeserializeObject<UserInfoResponse>(responseContent);

            Username = responseModel.username;
            Console.WriteLine($"Set username to {Username}");
            UserInfoChanged?.Invoke();

            return true;
        }

        public async Task LoginAsync(string username, string password)
        {
            LoginResponse response = await HttpClient.PostJsonAsync<LoginResponse>(Url("/accounts/login"), new LoginRequest(username, password));
            if (response.BearerToken != null)
            {
                SetAuthorization(response.BearerToken);
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
