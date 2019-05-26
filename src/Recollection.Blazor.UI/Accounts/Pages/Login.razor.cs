using Microsoft.AspNetCore.Components;
using Neptuo.Recollection.Accounts.Components;
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
        [CascadingParameter]
        public UserStateModel UserState { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        protected override void OnInit()
        {
            base.OnInit();
        }

        protected async Task LoginAsync()
        {
            if (UserState == null)
                Console.WriteLine("UserState is null!");
            else
                await UserState.LoginAsync(Username, Password);
        }
    }
}
