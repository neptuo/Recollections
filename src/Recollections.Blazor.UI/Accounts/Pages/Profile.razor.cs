using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages
{
    public partial class Profile
    {
        [Inject]
        protected Api Api { get; set; }

        [Parameter]
        public string UserId { get; set; }

        public string UserName { get; private set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            var response = await Api.GetProfileAsync(UserId);
            UserName = response.UserName;
        }
    }
}
