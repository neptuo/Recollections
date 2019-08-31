using Microsoft.AspNetCore.Components;
using Neptuo;
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

        [Inject]
        protected Json Json { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        [Parameter]
        protected string EntryId { get; set; }

        private EntryModel original;
        protected EntryModel Model { get; set; }
        protected List<ImageModel> Images { get; set; }
        protected List<UploadImageModel> UploadProgress { get; } = new List<UploadImageModel>();

        protected async override Task OnInitAsync()
        {
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

        protected async Task SaveAsync()
        {
            if (original.Equals(Model))
            {
                Console.WriteLine("Models are equal.");
                return;
            }

            Console.WriteLine("Saving model.");
            await Api.UpdateAsync(Model);
            UpdateOriginal();
            StateHasChanged();
        }

        private void UpdateOriginal() => original = Model.Clone();

        protected async Task OnUploadProgressAsync(IReadOnlyCollection<FileUploadProgress> progresses)
        {
            UploadProgress.Clear();
            if (progresses.All(p => p.Status == "done" || p.Status == "error"))
            {
                await LoadImagesAsync();
            }
            else
            {
                foreach (var progress in progresses)
                {
                    ImageModel image = null;
                    if (progress.Status == "done" && progress.ResponseText != null)
                        image = Json.Deserialize<ImageModel>(progress.ResponseText);

                    UploadProgress.Add(new UploadImageModel(progress, image));
                }
            }

            StateHasChanged();
        }

        protected string GetUploadErrorMessage(int statusCode)
        {
            switch (statusCode)
            {
                case 400:
                    return "File is of not supported type or too large.";
                default:
                    return "Unknown reason.";
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

        protected LocationModel SelectedLocation { get; set; }
        protected Modal LocationEdit { get; set; }

        protected void OnLocationSelected(int index)
        {
            SelectedLocation = Model.Locations[index];
            LocationEdit.Show();
            StateHasChanged();
        }

        protected async Task DeleteSelectedLocationAsync()
        {
            Model.Locations.Remove(SelectedLocation);
            await SaveAsync();
            LocationEdit.Hide();
        }
    }

    public class UploadImageModel
    {
        public FileUploadProgress Progress { get; }
        public ImageModel Image { get; }

        public bool IsSuccess => Progress.Status == "done" && Image != null;

        public string Description
        {
            get
            {
                if (Progress.Status == "done")
                    return "Uploaded";
                else if (Progress.Status == "current")
                    return $"{Progress.Precentual}%";
                else if (Progress.Status == "error")
                    return "Error";
                else if (Progress.Status == "pending")
                    return "Waiting";
                else
                    return "Unknown...";
            }
        }

        public UploadImageModel(FileUploadProgress progress, ImageModel image)
        {
            Ensure.NotNull(progress, "progress");
            Progress = progress;
            Image = image;
        }
    }
}
