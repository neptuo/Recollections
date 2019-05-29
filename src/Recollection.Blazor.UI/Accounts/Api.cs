using Microsoft.AspNetCore.Components;
using Neptuo;
using Neptuo.Activators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Accounts
{
    public class Api
    {
        private readonly HttpClient http;
        private readonly UrlResolver urlResolver;

        public AuthenticationHeaderValue Authorization
        {
            get => http.DefaultRequestHeaders.Authorization;
            set => http.DefaultRequestHeaders.Authorization = value;
        }

        public Api(HttpClient http, UrlResolver urlResolver)
        {
            Ensure.NotNull(http, "http");
            Ensure.NotNull(urlResolver, "urlResolver");
            this.http = http;
            this.urlResolver = urlResolver;
        }

        public Task<LoginResponse> LoginAsync(LoginRequest request) 
            => http.PostJsonAsync<LoginResponse>(urlResolver("/accounts/login"), request);

        public Task<RegisterResponse> RegisterAsync(RegisterRequest request) 
            => http.PostJsonAsync<RegisterResponse>(urlResolver("/accounts/register"), request);

        public async Task<UserInfoResponse> GetInfoAsync()
        {
            HttpResponseMessage response = await http.GetAsync(urlResolver("/accounts/info"));
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            string responseContent = await response.Content.ReadAsStringAsync();
            UserInfoResponse responseModel = SimpleJson.SimpleJson.DeserializeObject<UserInfoResponse>(responseContent);
            return responseModel;
        }
    }
}
