﻿using Microsoft.AspNetCore.Components;
using Neptuo.Exceptions.Handlers;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
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
    public partial class EntryDetail
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
        protected IFreeLimitsNotifier FreeLimitsNotifier { get; set; }

        [Inject]
        protected ILog<EntryDetail> Log { get; set; }

        [Inject]
        protected PropertyCollection Properties { get; set; }

        [Inject]
        protected IExceptionHandler ExceptionHandler { get; set; }

        private string previousEntryId;

        [Parameter]
        public string EntryId { get; set; }

        protected ElementReference Container { get; set; }

        private EntryModel original;
        protected EntryModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected List<ImageModel> Images { get; set; }
        protected EntryStoryModel Story { get; set; }
        protected List<EntryBeingModel> Beings { get; } = new List<EntryBeingModel>();
        protected string BeingsTitle => Beings.Count > 0 ? String.Join(", ", Beings.Select(b => b.Name)) : null;
        protected bool HasStory => Story != null && Story.StoryId != null;
        protected string StoryTitle
        {
            get
            {
                string title = null;
                if (HasStory)
                {
                    title = Story.StoryTitle;
                    if (Story.ChapterId != null)
                        title += " - " + Story.ChapterTitle;
                }

                return title;
            }
        }
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();
        protected int MarkerCount => Markers.Count(m => m.Longitude != null && m.Latitude != null);
        protected List<UploadImageModel> UploadProgress { get; } = new List<UploadImageModel>();
        protected List<UploadErrorModel> UploadErrors { get; } = new List<UploadErrorModel>();
        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();
        protected Gallery Gallery { get; set; }
        protected List<GalleryModel> GalleryItems { get; } = new List<GalleryModel>();

        protected async override Task OnInitializedAsync()
        {
            Log.Debug("OnInitializedAsync");

            await base.OnInitializedAsync();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            previousEntryId = EntryId;
            return base.SetParametersAsync(parameters);
        }

        protected async override Task OnParametersSetAsync()
        {
            if (previousEntryId != EntryId) 
            {
                await LoadAsync();
                await LoadImagesAsync();
                await LoadStoryAsync();
                await LoadBeingsAsync();
            }

        }

        private async Task LoadStoryAsync()
            => Story = await Api.GetEntryStoryAsync(EntryId);

        private async Task LoadBeingsAsync()
        {
            Beings.Clear();
            Beings.AddRange(await Api.GetEntryBeingsAsync(EntryId));
        }

        private async Task LoadAsync()
        {
            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetEntryAsync(EntryId);

            UpdateOriginal();

            Log.Debug($"Entry user permission '{userPermission}'.");
            Permissions.IsEditable = UserState.IsEditable && userPermission == Permission.CoOwner;
            Permissions.IsOwner = UserState.UserId == Owner.Id;

            var imagesCount = Images?.Count ?? 0;
            Markers.RemoveRange(imagesCount, Markers.Count - imagesCount);
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

            GalleryItems.Clear();
            foreach (var image in Images)
            {
                GalleryItems.Add(new GalleryModel()
                {
                    Title = image.Name,
                    Width = image.Preview.Width,
                    Height = image.Preview.Height
                });
            }
        }

        protected async Task<Stream> OnGetImageDataAsync(int index)
        {
            if (index > Images.Count)
                return null;

            var image = Images[index];
            Log.Debug($"Get image for gallery at '{index}' (count '{Images.Count}'), URL is '{image.Preview.Url}'.");

            var stream = await Api.GetImageDataAsync(image.Preview.Url);
            Log.Debug($"Got image data for gallery at '{index}'");

            return stream;
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

        protected async Task SaveLocationsAsync()
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

            await SaveAsync();

            // Reload data as altitude might have been computed
            await LoadAsync();
        }

        protected async Task SaveAsync()
        {
            if (original.Equals(Model))
            {
                Log.Debug("Models are equal.");
                return;
            }

            Log.Debug("Saving model.");
            await Api.UpdateEntryAsync(Model);
            UpdateOriginal();
            StateHasChanged();
        }

        private void UpdateOriginal() => original = Model.Clone();

        protected Modal UploadError { get; set; }

        protected async Task OnUploadProgressAsync(IReadOnlyCollection<FileUploadProgress> progresses)
        {
            Console.WriteLine("OnUploadProgressAsync: " + Json.Serialize(progresses));

            UploadProgress.Clear();
            if (progresses.All(p => p.Status == "done" || p.Status == "error"))
            {
                UploadErrors.Clear();
                UploadErrors.AddRange(progresses.Where(p => p.Status == "error").Select(p => new UploadErrorModel(p)));
                if (UploadErrors.Count > 0)
                {
                    if (UploadErrors.All(e => e.Progress.StatusCode == 402))
                        FreeLimitsNotifier.Show();
                    else
                        UploadError.Show();
                }

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
                await Api.DeleteEntryAsync(Model.Id);
                Navigator.OpenTimeline();
            }
        }

        protected int SelectedLocationIndex { get; set; }
        protected LocationModel SelectedLocation { get; set; }
        protected LocationEdit LocationEdit { get; set; }

        protected void OnLocationSelected(int index)
        {
            Log.Debug($"Marker selected '{index}'.");

            if (index < Images.Count)
            {
                var image = Images[index];

                Log.Debug($"Selected image '{image.Id}'.");

                Navigator.OpenImageDetail(EntryId, image.Id);
            }
            else
            {
                index -= Images.Count;
                if (index < Model.Locations.Count)
                {
                    Log.Debug($"Selected location '{index}': {Model.Locations[index]}.");

                    SelectedLocationIndex = index;
                    SelectedLocation = Model.Locations[index];
                    LocationEdit.Show(SelectedLocation);
                    StateHasChanged();
                }
            }
        }

        protected async Task DeleteSelectedLocationAsync()
        {
            Model.Locations.Remove(SelectedLocation);
            Markers.RemoveAt(SelectedLocationIndex + Images.Count);
            LocationEdit.Hide();
            await SaveAsync();
        }

        protected Task SaveSelectedLocationAsync(LocationModel model)
        {
            MapMarkerModel marker = Markers[SelectedLocationIndex];
            marker.Latitude = model.Latitude;
            marker.Longitude = model.Longitude;
            marker.Altitude = model.Altitude;
            LocationEdit.Hide();
            return SaveAsync();
        }

        protected StoryPicker StoryPicker { get; set; }

        protected void SelectStory() => StoryPicker.Show(Story?.StoryId, Story?.ChapterId);

        protected async Task StorySelectedAsync(EntryStoryModel model)
        {
            if (!await Api.UpdateEntryStoryAsync(EntryId, model))
                StoryPicker.SetErrorMessage("Missing required co-owner permission to select the story");
            else
                await LoadStoryAsync();
            
            StateHasChanged();
        }

        protected BeingPicker BeingPicker { get; set; }

        protected void SelectBeing() => BeingPicker.Show(Beings.Select(b => b.Id));

        protected async Task BeingSelectedAsync(List<string> beingIds)
        {
            await Api.UpdateEntryBeingsAsync(EntryId, beingIds);
            await LoadBeingsAsync();
        }
    }

    public class UploadImageModel
    {
        public FileUploadProgress Progress { get; }
        public ImageModel Image { get; }

        public bool IsSuccess => IsDone && Image != null;
        public bool IsDone => Progress.Status == "done";
        public bool IsError => Progress.Status == "error";
        public bool IsPending => Progress.Status == "pending";
        public bool IsCurrent => Progress.Status == "current";

        public string Description
        {
            get
            {
                if (Progress.Status == "done")
                    return "Uploaded";
                else if (IsCurrent && Progress.Percentual == 100)
                    return $"Saving...";
                else if (IsCurrent)
                    return $"{Progress.Percentual}%";
                else if (IsError)
                    return "Failed to upload";
                else if (IsPending)
                    return "Waiting";
                else
                    return "Unknown...";
            }
        }

        public string StatusCssClass
        {
            get
            {
                if (IsCurrent)
                    return Progress.Percentual == 0 ? "loading-circle" : $"text-primary";
                else
                    return String.Empty;
            }
        }

        public EntryImagePlaceHolderState PlaceHolderState 
        {
            get 
            {
                if (IsError)
                    return EntryImagePlaceHolderState.Error;
                
                if (IsPending)
                    return EntryImagePlaceHolderState.Pending;

                if (IsCurrent && Progress.Percentual > 0 && Progress.Percentual < 100)
                    return EntryImagePlaceHolderState.Progress;

                if (IsCurrent && Progress.Percentual == 100)
                    return EntryImagePlaceHolderState.Finished;

                if (IsDone)
                    return EntryImagePlaceHolderState.Success;

                return EntryImagePlaceHolderState.None;
            }
        }

        public UploadImageModel(FileUploadProgress progress, ImageModel image)
        {
            Ensure.NotNull(progress, "progress");
            Progress = progress;
            Image = image;
        }
    }

    public class UploadErrorModel
    {
        public FileUploadProgress Progress { get; }

        public UploadErrorModel(FileUploadProgress progress)
        {
            Ensure.NotNull(progress, "progress");
            Progress = progress;
        }

        public string Description
        {
            get
            {
                if (Progress.StatusCode == 400)
                    return "File is too large.";
                else if (Progress.StatusCode == 402)
                    return "Premium required.";
                else
                    return "Unexpected server error.";
            }
        }
    }
}
