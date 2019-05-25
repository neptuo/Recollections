using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Accounts.Pages
{
    public class LoginModel : ComponentBase
    {
        [Inject]
        public HttpClient HttpClient { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        protected override void OnInit()
        {
            base.OnInit();

            //HttpClient.BaseAddress = new Uri("http://localhost:62198/");
        }

        protected async Task LoginAsync()
        {
            LoginResponse response = await HttpClient.PostJsonAsync<LoginResponse>("http://localhost:62198/api/accounts/login", new LoginRequest(Username, Password));
            Console.WriteLine($"BearerToken: {response.BearerToken}");
        }
    }
}
