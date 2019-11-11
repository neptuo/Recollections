using Microsoft.AspNetCore.Components;
using Neptuo;
using Neptuo.Logging;
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

        [Inject]
        protected ILog<EntryDetailModel> Log { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        [Parameter]
        public string EntryId { get; set; }

        private EntryModel original;
        protected EntryModel Model { get; set; }
        protected List<ImageModel> Images { get; set; }
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();
        protected List<UploadImageModel> UploadProgress { get; } = new List<UploadImageModel>();

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await UserState.EnsureAuthenticatedAsync();

            await LoadAsync();
            await LoadImagesAsync();
        }

        private async Task LoadAsync()
        {
            Model = await Api.GetDetailAsync(EntryId);
            UpdateOriginal();

            Markers.Clear();
            foreach (var location in Model.Locations)
            {
                Markers.Add(new MapMarkerModel
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Altitude = location.Altitude,
                    IsEditable = true
                });
            }

            if (Log.IsDebugEnabled())
            {
                Log.Debug($"Load, markers: {Markers.Count}, entry locations: {Model.Locations.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }
        }

        private async Task LoadImagesAsync()
        {
            int imagesCount = 0;
            if (Images != null)
                imagesCount = Images.Count;

            Images = await Api.GetImagesAsync(EntryId);

            if (Log.IsDebugEnabled())
            {
                Log.Debug($"LoadImages, previous images: {imagesCount}, markers: {Markers.Count}, entry locations: {Model.Locations.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }

            if (imagesCount > 0)
                Markers.RemoveRange(0, imagesCount);

            if (Log.IsDebugEnabled())
            {
                Log.Debug($"LoadImages.Cleared, markers: {Markers.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }

            for (int i = 0; i < Images.Count; i++)
            {
                var image = Images[i];

                Markers.Insert(i, new MapMarkerModel
                {
                    Latitude = image.Location.Latitude,
                    Longitude = image.Location.Longitude,
                    Altitude = image.Location.Altitude,
                    DropColor = "blue",
                    Title = image.Name
                });
            }

            if (Log.IsDebugEnabled())
            {
                Log.Debug($"LoadImages.Final, markers: {Markers.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }
        }

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

        protected Task SaveLocationsAsync()
        {
            void Map(MapMarkerModel marker, LocationModel location)
            {
                location.Latitude = marker.Latitude;
                location.Longitude = marker.Longitude;
                location.Altitude = marker.Altitude;
            }

            if (Log.IsDebugEnabled())
            {
                Log.Debug($"SaveLocations, Model: {Model.Locations.Count}, Images: {Images.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }

            for (int i = Images.Count; i < Markers.Count; i++)
            {
                MapMarkerModel marker = Markers[i];
                LocationModel location;

                int modelIndex = i - Images.Count;
                if (modelIndex < Model.Locations.Count)
                    location = Model.Locations[modelIndex];
                else
                    Model.Locations.Add(location = new LocationModel());

                Map(marker, location);
            }

            if (Log.IsDebugEnabled())
            {
                Log.Debug($"SaveLocations.Final, Model: {Model.Locations.Count}.");
                Log.Debug(Json.Serialize(Model.Locations));
            }

            return SaveAsync();
        }

        protected async Task SaveAsync()
        {
            if (original.Equals(Model))
            {
                Log.Debug("Models are equal.");
                return;
            }

            Log.Debug("Saving model.");
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

        public async Task DeleteAsync()
        {
            if (await Navigator.AskAsync($"Do you really want to delete entry '{Model.Title}'?"))
            {
                await Api.DeleteAsync(Model.Id);
                Navigator.OpenTimeline();
            }
        }

        protected int SelectedLocationIndex { get; set; }
        protected LocationModel SelectedLocation { get; set; }
        protected Modal LocationEdit { get; set; }

        protected void OnLocationSelected(int index)
        {
            index -= Images.Count;
            if (index < Model.Locations.Count)
            {
                Log.Debug($"Selected location '{index}': {Model.Locations[index]}.");

                SelectedLocationIndex = index;
                SelectedLocation = Model.Locations[index];
                LocationEdit.Show();
                StateHasChanged();
            }
        }

        protected async Task DeleteSelectedLocationAsync()
        {
            Model.Locations.Remove(SelectedLocation);
            Markers.RemoveAt(SelectedLocationIndex + Images.Count);
            LocationEdit.Hide();
            await SaveAsync();
        }

        protected Task SaveSelectedLocationAsync()
        {
            MapMarkerModel marker = Markers[SelectedLocationIndex];
            marker.Latitude = SelectedLocation.Latitude;
            marker.Longitude = SelectedLocation.Longitude;
            marker.Altitude = SelectedLocation.Altitude;
            LocationEdit.Hide();
            return SaveAsync();
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
                else if (Progress.Status == "current" && Progress.Percentual == 100)
                    return $"Saving...";
                else if (Progress.Status == "current")
                    return $"{Progress.Percentual}%";
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
