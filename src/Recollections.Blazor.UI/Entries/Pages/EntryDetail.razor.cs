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
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();
        protected List<UploadImageModel> UploadProgress { get; } = new List<UploadImageModel>();

        protected async override Task OnInitAsync()
        {
            await base.OnInitAsync();
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
        }

        private async Task LoadImagesAsync()
        {
            int imagesCount = 0;
            if (Images != null)
                imagesCount = Images.Count;

            Images = await Api.GetImagesAsync(EntryId);

            Console.WriteLine($"LoadImages, previous images: {imagesCount}, markers: {Markers.Count}, entry locations: {Model.Locations.Count}.");
            Console.WriteLine(Json.Serialize(Markers));

            if (imagesCount > 0)
                Markers.RemoveRange(Model.Locations.Count, imagesCount);

            Console.WriteLine($"LoadImages.Cleared, markers: {Markers.Count}.");
            Console.WriteLine(Json.Serialize(Markers));

            foreach (var image in Images)
            {
                Markers.Add(new MapMarkerModel
                {
                    Latitude = image.Location.Latitude,
                    Longitude = image.Location.Longitude,
                    Altitude = image.Location.Altitude,
                    DropColor = "blue",
                    Title = image.Name
                });
            }

            Console.WriteLine($"LoadImages.Final, markers: {Markers.Count}.");
            Console.WriteLine(Json.Serialize(Markers));
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

            Console.WriteLine(Json.Serialize(Markers));
            Console.WriteLine($"Model: {Model.Locations.Count}, Images: {Images.Count}.");

            int existingCount = Model.Locations.Count + Images.Count;
            for (int i = 0; i < Markers.Count; i++)
            {
                if (i < Model.Locations.Count)
                {
                    MapMarkerModel marker = Markers[i];
                    LocationModel location = Model.Locations[i];
                    Map(marker, location);
                }
                else if (i >= existingCount)
                {
                    MapMarkerModel marker = Markers[i];
                    int newIndex = Model.Locations.Count;
                    if (i != newIndex)
                    {
                        Console.WriteLine($"SaveLocations, removing marker at '{i}' and inserting at '{newIndex}'.");
                        Markers.RemoveAt(i);
                        Markers.Insert(newIndex, marker);
                    }

                    LocationModel location = new LocationModel();
                    Model.Locations.Add(location);
                    Map(marker, location);
                }
            }

            Console.WriteLine(Json.Serialize(Model.Locations));
            return SaveAsync();
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
            if (index < Model.Locations.Count)
            {
                Console.WriteLine($"Selected location '{index}': {Model.Locations[index]}.");

                SelectedLocationIndex = index;
                SelectedLocation = Model.Locations[index];
                LocationEdit.Show();
                StateHasChanged();
            }
        }

        protected async Task DeleteSelectedLocationAsync()
        {
            Model.Locations.Remove(SelectedLocation);
            Markers.RemoveAt(SelectedLocationIndex);
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
