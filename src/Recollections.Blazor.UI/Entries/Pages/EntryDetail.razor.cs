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
    public class EntryDetailModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        [Parameter]
        protected string EntryId { get; set; }

        private EntryModel original;
        protected EntryModel Model { get; set; }
        protected List<ImageModel> Images { get; set; }
        protected string UploadButtonText { get; set; }
        protected string UploadError { get; set; }

        protected async override Task OnInitAsync()
        {
            ResetUploadButtonText();

            await base.OnInitAsync();
            await UserState.EnsureAuthenticatedAsync();

            Model = await Api.GetDetailAsync(EntryId);
            UpdateOriginal();

            await LoadImagesAsync();
        }

        private async Task LoadImagesAsync()
            => Images = await Api.GetImagesAsync(EntryId);

        protected async Task SaveTitleAsync(string value)
        {
            if (String.IsNullOrEmpty(value))
                value = null;

            Model.Title = value;
            await SaveAsync();
        }

        protected async Task SaveTextAsync(string value)
        {
            Model.Text = value;
            await SaveAsync();
        }

        protected async Task SaveWhenAsync(DateTime value)
        {
            Model.When = value;
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            if (original.Equals(Model))
                return;

            await Api.UpdateAsync(Model);
            UpdateOriginal();
        }

        private void UpdateOriginal() => original = Model.Clone();

        protected async Task OnUploadProgressAsync(FileUploadProgress e)
        {
            SetUploadError(null);

            if (e.Completed == e.Total)
            {
                await LoadImagesAsync();
                ResetUploadButtonText();
            }
            else
            {
                UploadButtonText = $"{FileUploadModel.DefaultText} - {e.Completed} / {e.Total}";
            }

            StateHasChanged();
        }

        private void ResetUploadButtonText()
        {
            UploadButtonText = FileUploadModel.DefaultText;
        }

        protected async Task OnUploadErrorAsync(FileUploadError e)
        {
            ResetUploadButtonText();
            SetUploadError(e);

            await LoadImagesAsync();

            StateHasChanged();
        }

        private void SetUploadError(FileUploadError e)
        {
            if (e != null)
            {
                string reason;
                switch (e.StatusCode)
                {
                    case 400:
                        reason = "File is of not supported type or too large.";
                        break;
                    default:
                        reason = "Unknown reason.";
                        break;
                }

                UploadError = $"Error during file upload ({e.Completed + 1} / {e.Total}): {reason}";
            }
            else
            {
                UploadError = null;
            }
        }

        public async Task DeleteAsync()
        {
            if (await Navigator.AskAsync($"Do you really want to delete entry '{Model.Title}'?"))
            {
                await Api.DeleteAsync(Model.Id);
                Navigator.OpenTimeline();
            }
        }
    }
}
