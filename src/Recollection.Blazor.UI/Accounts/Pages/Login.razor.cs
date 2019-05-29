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
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        public bool IsPersistent { get; set; }

        protected async Task LoginAsync() => await UserState.LoginAsync(UserName, Password, IsPersistent);
    }
}
