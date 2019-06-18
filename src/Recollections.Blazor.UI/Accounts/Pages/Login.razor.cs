using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages
{
    public class LoginModel : ComponentBase
    {
        [CascadingParameter]
        public UserStateModel UserState { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        public bool IsPersistent { get; set; }

        public List<string> ErrorMessages { get; } = new List<string>();

        protected async Task LoginAsync()
        {
            ErrorMessages.Clear();
            if (!await UserState.LoginAsync(UserName, Password, IsPersistent))
                ErrorMessages.Add("Invalid combination of username and password.");
        }
    }
}
