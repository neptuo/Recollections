using Microsoft.AspNetCore.Components;
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
    public partial class EntryDetail : IDisposable
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
        protected ILog<EntryDetail> Log { get; set; }

        [Inject]
        protected PropertyCollection Properties { get; set; }

        [Inject]
        protected IExceptionHandler ExceptionHandler { get; set; }

        [Inject]
        protected FileUploader FileUploader { get; set; }

        private IDisposable previousUploadListener;
        private string previousEntryId;

        [Parameter]
        public string EntryId { get; set; }

        protected ElementReference Container { get; set; }

        private EntryModel original;
        protected EntryModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected List<MediaModel> Media { get; set; }
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
        protected List<FileUploadProgress> UploadProgress { get; } = [];
        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();
        protected Gallery Gallery { get; set; }
        protected List<GalleryModel> GalleryItems { get; } = new List<GalleryModel>();

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
                await LoadStoryAsync();
                await LoadBeingsAsync();
                await LoadMediaAsync();

                previousUploadListener?.Dispose();
                previousUploadListener = FileUploader.AddProgressListener("entry", EntryId, (progresses) => _ = OnUploadProgressAsync(progresses));
            }
        }
        
        public void Dispose()
        {
            previousUploadListener?.Dispose();
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

            var mediaCount = Media?.Count ?? 0;
            Markers.RemoveRange(mediaCount, Markers.Count - mediaCount);
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

        private async Task LoadMediaAsync()
        {
            int mediaCount = 0;
            if (Media != null)
                mediaCount = Media.Count;

            Media = await Api.GetMediaAsync(EntryId);
            if (Log.IsDebugEnabled())
            {
                Log.Debug($"LoadMediaAsync, previous media: {mediaCount}, markers: {Markers.Count}, entry locations: {Model.Locations.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }

            if (mediaCount > 0)
                Markers.RemoveRange(0, mediaCount);

            if (Log.IsDebugEnabled())
            {
                Log.Debug($"LoadMediaAsync.Cleared, markers: {Markers.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }

            for (int i = 0; i < Media.Count; i++)
            {
                var item = Media[i];
                if (item.Image != null)
                {
                    Markers.Insert(i, new MapMarkerModel
                    {
                        Latitude = item.Image.Location.Latitude,
                        Longitude = item.Image.Location.Longitude,
                        Altitude = item.Image.Location.Altitude,
                        DropColor = "blue",
                        Title = item.Image.Name
                    });
                }
                else if (item.Video != null)
                {
                    Markers.Insert(i, new MapMarkerModel
                    {
                        Latitude = item.Video.Location.Latitude,
                        Longitude = item.Video.Location.Longitude,
                        Altitude = item.Video.Location.Altitude,
                        DropColor = "blue",
                        Title = item.Video.Name
                    });
                }
            }

            if (Log.IsDebugEnabled())
            {
                Log.Debug($"LoadMediaAsync.Final, markers: {Markers.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }

            GalleryItems.Clear();
            foreach (var item in Media)
            {
                if (item.Image != null)
                {
                    GalleryItems.Add(new GalleryModel()
                    {
                        Type = "image",
                        Title = item.Image.Name,
                        Width = item.Image.Preview.Width,
                        Height = item.Image.Preview.Height
                    });
                }
                else if (item.Video != null)
                {
                    GalleryItems.Add(new GalleryModel()
                    {
                        Type = "video",
                        Title = item.Video.Name,
                        Width = item.Video.Preview.Width,
                        Height = item.Video.Preview.Height,
                        ContentType = item.Video.ContentType,
                    });
                }
            }
        }

        protected async Task<Stream> OnGetImageDataAsync(int index, string type)
        {
            if (index >= Media.Count)
                return null;

            var item = Media[index];
            if (item.Image != null)
            {
                var image = item.Image;
                Log.Debug($"Get image for gallery at '{index}' (count '{Media.Count}'), URL is '{image.Preview.Url}'.");

                var stream = await Api.GetImageDataAsync(image.Preview.Url);
                Log.Debug($"Got image data for gallery at '{index}'");

                return stream;
            }
            else if (item.Video != null)
            {
                if (type == "original")
                    return await Api.GetVideoDataAsync(item.Video.Original.Url);
                else
                    return await Api.GetVideoDataAsync(item.Video.Preview.Url);
            }

            return null;
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
                Log.Debug($"SaveLocations, Model: {Model.Locations.Count}, Media: {Media.Count}.");
                Log.Debug(Json.Serialize(Markers));
            }

            for (int i = Media.Count; i < Markers.Count; i++)
            {
                MapMarkerModel marker = Markers[i];
                LocationModel location;

                int modelIndex = i - Media.Count;
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

        protected async Task OnUploadProgressAsync(IReadOnlyCollection<FileUploadProgress> progresses)
        {
            Log.Debug($"{EntryId}: OnUploadProgressAsync: " + Json.Serialize(progresses));

            UploadProgress.Clear();
            if (progresses.All(p => p.Status == "done" || p.Status == "error"))
            {
                Log.Debug($"{EntryId}: All uploads done, reloading media.");
                await LoadMediaAsync();
            }
            else
            {
                foreach (var progress in progresses)
                {
                    if (progress.Status == "done" && progress.ResponseText != null)
                    {
                        var media = progress.Tag as MediaModel;
                        if (media == null)
                            media = Json.Deserialize<MediaModel>(progress.ResponseText);

                        progress.Tag = media;
                    }

                    UploadProgress.Add(progress);
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

            if (index < Media.Count)
            {
                var media = Media[index];
                if (media.Image != null)
                {
                    var image = media.Image;
                    Log.Debug($"Selected image '{image.Id}'.");

                    Navigator.OpenImageDetail(EntryId, image.Id);
                }
                else if (media.Video != null)
                {
                    var video = media.Video;
                    Log.Debug($"Selected video '{video.Id}'.");

                    Navigator.OpenVideoDetail(EntryId, video.Id);
                }
            }
            else
            {
                index -= Media.Count;
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
            Markers.RemoveAt(SelectedLocationIndex + Media.Count);
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

        private EntryMediaPlaceHolderState GetImagePlaceHolderState(FileUploadProgress progress)
        {
            if (progress.IsError)
                return EntryMediaPlaceHolderState.Error;

            if (progress.IsPending)
                return EntryMediaPlaceHolderState.Pending;

            if (progress.IsCurrent && progress.Percentual > 0 && progress.Percentual < 100)
                return EntryMediaPlaceHolderState.Progress;

            if (progress.IsCurrent && progress.Percentual == 100)
                return EntryMediaPlaceHolderState.Finished;

            if (progress.IsDone)
                return EntryMediaPlaceHolderState.Success;

            return EntryMediaPlaceHolderState.None;
        }
        
        private async Task OnGalleryOpenInfoAsync(int index)
        {
            await Gallery.CloseAsync();

            var item = Media[index];
            if (item.Image != null)
                Navigator.OpenImageDetail(EntryId, item.Image.Id);
            else if (item.Video != null)
                Navigator.OpenVideoDetail(EntryId, item.Video.Id);
        }
    }
}
