using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Accounts.Pages
{
    public class RegisterModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected IUriHelper Uri { get; set; }

        public List<string> ErrorMessages { get; } = new List<string>();

        public string UserName { get; set; }
        public string Password { get; set; }

        public async Task RegisterAsync()
        {
            RegisterResponse response = await Api.RegisterAsync(new RegisterRequest(UserName, Password));
            if (response.IsSuccess)
            {
                UserName = null;
                Password = null;
                Uri.NavigateTo("/login");
            }
            else
            {
                ErrorMessages.AddRange(response.ErrorMessages);
            }
        }
    }
}
