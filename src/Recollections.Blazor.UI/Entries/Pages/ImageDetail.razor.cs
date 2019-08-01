using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public class ImageDetailModel : ComponentBase
    {
        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected Downloader Downloader { get; set; }

        [Parameter]
        protected string EntryId { get; set; }

        [Parameter]
        protected string ImageId { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        private ImageModel original;
        protected ImageModel Model { get; set; }

        protected async override Task OnInitAsync()
        {
            await base.OnInitAsync();
            await UserState.EnsureAuthenticatedAsync();

            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Model = await Api.GetImageAsync(EntryId, ImageId);
            UpdateOriginal();
        }

        protected async Task SaveNameAsync(string value)
        {
            Model.Name = value;
            await SaveAsync();
        }

        protected async Task SaveDescriptionAsync(string value)
        {
            if (String.IsNullOrEmpty(value))
                value = null;

            Model.Description = value;
            await SaveAsync();
        }

        protected async Task SaveWhenAsync(DateTime value)
        {
            Model.When = value;
            await SaveAsync();
        }

        protected async Task SaveLocationAsync(LocationModel value)
        {
            Model.Location = value;
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            if (original.Equals(Model))
                return;

            await Api.UpdateImageAsync(EntryId, Model);
            UpdateOriginal();
        }

        private void UpdateOriginal() => original = Model.Clone();

        protected async Task DeleteAsync()
        {
            if (await Navigator.AskAsync($"Do you really want to delete this image?"))
            {
                await Api.DeleteImageAsync(EntryId, ImageId);
                Navigator.OpenEntryDetail(EntryId);
            }
        }

        protected async Task SetLocationOriginalAsync()
        {
            await Api.SetImageLocationFromOriginalAsync(EntryId, ImageId);
            await LoadAsync();
        }

        protected Task DownloadOriginalAsync() => Downloader.FromUrlAsync(Model.Name, Api.ResolveUrl(Model.Original));
    }
}
