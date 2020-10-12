using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing.Components
{
    public partial class ShareButton
    {
        [Inject]
        protected Api Api { get; set; }

        [Parameter]
        public string EntryId { get; set; }

        protected bool AreItemsLoading { get; set; }
        protected Modal Modal { get; set; }
        protected List<ShareModel> Items { get; set; }

        protected ShareModel NewShare { get; } = new ShareModel();

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            AreItemsLoading = true;
            Items = await Api.GetEntryListAsync(EntryId);
            AreItemsLoading = false;
        }

        protected void OnToggle()
            => Modal.Show();

        protected async Task OnAddAsync()
        {
            if (String.IsNullOrEmpty(NewShare.UserName) || String.IsNullOrWhiteSpace(NewShare.UserName))
                NewShare.UserName = null;

            await Api.CreateEntryAsync(EntryId, NewShare);
            await LoadAsync();

            NewShare.UserName = null;
            NewShare.Permission = Permission.Read;
        }

        protected async Task OnDeleteAsync(ShareModel model)
        {
            await Api.DeleteEntryAsync(EntryId, model);
            await LoadAsync();
        }
    }
}
