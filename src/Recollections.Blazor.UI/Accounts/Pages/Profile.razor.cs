using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Sharing;
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

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        [Parameter]
        public string UserId { get; set; }

        protected ProfileModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await UserState.EnsureInitializedAsync();
        }

        public async override Task SetParametersAsync(ParameterView parameters)
        {
            var oldUserId = UserId;
            await base.SetParametersAsync(parameters);

            if (oldUserId != UserId)
                await LoadAsync();
        }

        protected async Task LoadAsync()
        {
            Model = null;
            Owner = null;

            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetProfileAsync(UserId);

            Permissions.IsEditable = UserState.IsEditable && userPermission == Permission.CoOwner;
            Permissions.IsOwner = UserState.UserId == UserId;

            StateHasChanged();
        }
    }
}
