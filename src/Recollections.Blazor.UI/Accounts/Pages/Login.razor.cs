using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages
{
    public partial class Login
    {
        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected ILog<Login> Log { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        public bool IsPersistent { get; set; } = true;

        public List<string> ErrorMessages { get; } = new List<string>();

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            await UserState.EnsureInitializedAsync();
            if (UserState.IsAuthenticated)
                Navigator.OpenTimeline();
        }

        protected async Task LoginAsync()
        {
            Log.Debug($"UserName: '{UserName}'");
            Log.Debug($"Password: '{Password.Length}'");

            ErrorMessages.Clear();
            if (!await UserState.LoginAsync(UserName, Password, IsPersistent))
                ErrorMessages.Add("Invalid combination of username and password.");
        }
    }
}
