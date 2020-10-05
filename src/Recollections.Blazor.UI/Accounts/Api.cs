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
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class Api
    {
        private readonly HttpClient http;
        private readonly Json json;
        private readonly TaskFaultHandler faultHandler;

        public AuthenticationHeaderValue Authorization
        {
            get => http.DefaultRequestHeaders.Authorization;
            set => http.DefaultRequestHeaders.Authorization = value;
        }

        public Api(IFactory<HttpClient> httpFactory, Json json, TaskFaultHandler faultHandler)
        {
            Ensure.NotNull(httpFactory, "httpFactory");
            Ensure.NotNull(json, "json");
            Ensure.NotNull(faultHandler, "faultHandler");
            this.http = httpFactory.Create();
            this.json = json;
            this.faultHandler = faultHandler;
        }

        public Task<LoginResponse> LoginAsync(LoginRequest request)
            => faultHandler.Wrap(http.PostJsonAsync<LoginRequest, LoginResponse>("accounts/login", request));

        public Task<RegisterResponse> RegisterAsync(RegisterRequest request) 
            => faultHandler.Wrap(http.PostJsonAsync<RegisterRequest, RegisterResponse >("accounts/register", request));

        public async Task<UserInfoResponse> GetInfoAsync()
        {
            try
            {
                HttpResponseMessage response = await http.GetAsync("accounts/info");
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException();

                string responseContent = await response.Content.ReadAsStringAsync();
                UserInfoResponse responseModel = json.Deserialize<UserInfoResponse>(responseContent);
                return responseModel;
            }
            catch (Exception e)
            {
                faultHandler.Handle(e);
                throw;
            }
        }

        public Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
            => faultHandler.Wrap(http.PostJsonAsync<ChangePasswordRequest, ChangePasswordResponse>("accounts/changepassword", request));

        public Task<UserDetailResponse> GetDetailAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<UserDetailResponse>("accounts/detail"));
    }
}
