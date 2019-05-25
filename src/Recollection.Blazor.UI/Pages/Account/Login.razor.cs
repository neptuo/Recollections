using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Pages.Account
{
    public class LoginModel : ComponentBase
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        protected async Task LoginAsync()
        {
            Console.WriteLine($"Login as '{Username}'.");
        }
    }
}
