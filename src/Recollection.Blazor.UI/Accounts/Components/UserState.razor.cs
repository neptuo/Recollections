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

        public string BearerToken { get; private set; }
        public string Username { get; private set; }

        public static string Url(string appRelative) => $"http://localhost:62198/api{appRelative}";

        private void SetAuthorization(string bearerToken)
        {
            BearerToken = bearerToken;
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        private void ClearAuthorization()
        {
            if (BearerToken != null)
            {
                BearerToken = null;
                HttpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        protected override async Task OnInitAsync()
        {
            HttpResponseMessage response = await HttpClient.GetAsync(Url("/accounts/info"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ClearAuthorization();
                Uri.NavigateTo("/login");
                return;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            UserInfoResponse responseModel = SimpleJson.SimpleJson.DeserializeObject<UserInfoResponse>(responseContent);

            Username = responseModel.Username;
        }

        public async Task LoginAsync(string username, string password)
        {
            LoginResponse response = await HttpClient.PostJsonAsync<LoginResponse>(Url("/accounts/login"), new LoginRequest(username, password));
            if (response.BearerToken != null)
            {
                SetAuthorization(response.BearerToken);
                Uri.NavigateTo("/");
            }
        }
    }
}
