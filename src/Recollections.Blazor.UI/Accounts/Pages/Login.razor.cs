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
        protected string UserName { get; set; }

        [Required]
        protected string Password { get; set; }

        protected bool IsPersistent { get; set; } = true;
        protected bool IsValid { get; set; } = true;

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
            Log.Debug($"Password: '{Password?.Length ?? 0}'");

            IsValid = true;
            if (String.IsNullOrEmpty(UserName) || String.IsNullOrEmpty(Password) || !await UserState.LoginAsync(UserName, Password, IsPersistent))
                IsValid = false;
        }
    }
}
