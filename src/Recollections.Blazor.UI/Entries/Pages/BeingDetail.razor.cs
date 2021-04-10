using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Entries.Components;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class BeingDetail
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        [Parameter]
        public string BeingId { get; set; }

        protected EntryPicker EntryPicker { get; set; }
        protected BeingModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();

        protected BeingIconPicker IconPicker { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await UserState.EnsureInitializedAsync();
            await LoadAsync();
        }

        protected async Task LoadAsync()
        {
            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetBeingAsync(BeingId);

            Permissions.IsEditable = userPermission == Permission.Write;
            Permissions.IsOwner = UserState.UserId == Model.UserId;
        }

        protected Task SaveAsync()
            => Api.UpdateBeingAsync(Model);

        protected Task SaveNameAsync(string title)
        {
            Model.Name = title;
            return SaveAsync();
        }

        protected Task SaveIconAsync(string icon)
        {
            Model.Icon = icon;
            return SaveAsync();
        }

        protected Task SaveTextAsync(string text)
        {
            Model.Text = text;
            return SaveAsync();
        }

        protected async Task DeleteAsync()
        {
            if (await Navigator.AskAsync($"Do you really want to delete being '{Model.Name}'?"))
            {
                await Api.DeleteBeingAsync(Model.Id);
                Navigator.OpenBeings();
            }
        }
    }
}
