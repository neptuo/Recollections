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

        protected string NewUserName { get; set; }
        protected Permission NewPermission { get; set; }

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
            if (String.IsNullOrEmpty(NewUserName) || String.IsNullOrWhiteSpace(NewUserName))
                NewUserName = null;

            await Api.CreateEntryAsync(EntryId, new ShareModel(NewUserName, NewPermission));
            await LoadAsync();
        }

        protected async Task OnDeleteAsync(ShareModel model)
        {
            await Api.DeleteEntryAsync(EntryId, model);
            await LoadAsync();
        }
    }
}
