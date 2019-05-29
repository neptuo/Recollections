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
        public string UserName { get; set; }
        public string Password { get; set; }

        public async Task RegisterAsync()
        {
            UserName = null;
            Password = null;
        }
    }
}
