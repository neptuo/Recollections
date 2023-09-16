﻿using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages
{
    public partial class Register
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        public List<string> ErrorMessages { get; } = new List<string>();

        public string UserName { get; set; }
        public string Password { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (UserState.IsAuthenticated)
                Navigator.OpenTimeline();
        }

        public async Task RegisterAsync()
        {
            RegisterResponse response = await Api.RegisterAsync(new RegisterRequest(UserName, Password));
            if (response.IsSuccess)
            {
                if (await UserState.LoginAsync(UserName, Password))
                    Navigator.OpenTimeline();
                
                UserName = null;
                Password = null;
            }
            else
            {
                ErrorMessages.AddRange(response.ErrorMessages);
            }
        }
    }
}
