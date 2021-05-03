using Microsoft.AspNetCore.Components;
using Neptuo;
using Neptuo.Activators;
using Neptuo.Recollections.Commons.Exceptions;
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
        private readonly TaskFaultHandler faultHandler;

        public AuthenticationHeaderValue Authorization
        {
            get => http.DefaultRequestHeaders.Authorization;
            set => http.DefaultRequestHeaders.Authorization = value;
        }

        public Api(IFactory<HttpClient> httpFactory, TaskFaultHandler faultHandler)
        {
            Ensure.NotNull(httpFactory, "httpFactory");
            Ensure.NotNull(faultHandler, "faultHandler");
            this.http = httpFactory.Create();
            this.faultHandler = faultHandler;
        }

        public Task<LoginResponse> LoginAsync(LoginRequest request)
            => faultHandler.Wrap(http.PostAsJsonAsync<LoginRequest, LoginResponse>("accounts/login", request));

        public Task<LoginResponse> LoginWithTokenAsync(LoginWithTokenRequest request)
            => faultHandler.Wrap(http.PostAsJsonAsync<LoginWithTokenRequest, LoginResponse>("accounts/login/token", request));

        public Task<RegisterResponse> RegisterAsync(RegisterRequest request) 
            => faultHandler.Wrap(http.PostAsJsonAsync<RegisterRequest, RegisterResponse>("accounts/register", request));

        public Task<UserInfoResponse> GetProfileAsync(string userId)
            => faultHandler.Wrap(http.GetFromJsonAsync<UserInfoResponse>($"profiles/{userId}"));

        public async Task<UserInfoResponse> GetInfoAsync()
        {
            try
            {
                HttpResponseMessage response = await http.GetAsync("accounts/info");
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException();

                UserInfoResponse responseModel = await response.Content.ReadFromJsonAsync<UserInfoResponse>();
                return responseModel;
            }
            catch (Exception e)
            {
                faultHandler.Handle(e);
                throw;
            }
        }

        public Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
            => faultHandler.Wrap(http.PostAsJsonAsync<ChangePasswordRequest, ChangePasswordResponse>("accounts/changepassword", request));

        public Task<UserDetailResponse> GetDetailAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<UserDetailResponse>("accounts/detail"));
    }
}
