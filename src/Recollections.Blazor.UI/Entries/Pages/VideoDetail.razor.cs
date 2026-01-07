using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
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
    public partial class VideoDetail
    {
        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected Downloader Downloader { get; set; }

        [Inject]
        protected ILog<VideoDetail> Log { get; set; }

        [Parameter]
        public string EntryId { get; set; }

        private string previousVideoId;

        [Parameter]
        public string VideoId { get; set; }

        private VideoModel original;
        protected VideoModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected EntryModel EntryModel { get; set; }
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        protected LocationEdit LocationEdit { get; set; }
        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();

        public override Task SetParametersAsync(ParameterView parameters)
        {
            previousVideoId = VideoId;
            return base.SetParametersAsync(parameters);
        }

        protected async override Task OnParametersSetAsync()
        {
            if (previousVideoId != VideoId)
                await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetVideoAsync(EntryId, VideoId);

            UpdateOriginal();

            Permissions.IsOwner = Model.UserId == UserState.UserId;
            Permissions.IsEditable = userPermission == Permission.CoOwner;

            Markers.Clear();
            Markers.Add(new MapMarkerModel
            {
                Latitude = Model.Location.Latitude,
                Longitude = Model.Location.Longitude,
                Altitude = Model.Location.Altitude,
                IsEditable = true
            });

            if (EntryId != null)
                (EntryModel, _, _) = await Api.GetEntryAsync(EntryId);
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

        protected async Task SaveLocationAsync(LocationModel model = null)
        {
            if (model == null)
            {
                Model.Location.Latitude = Markers[0].Latitude;
                Model.Location.Longitude = Markers[0].Longitude;
                Model.Location.Altitude = Markers[0].Altitude;
            }
            else
            {
                Model.Location.Latitude = Markers[0].Latitude = model.Latitude;
                Model.Location.Longitude = Markers[0].Longitude = model.Longitude;
                Model.Location.Altitude = Markers[0].Altitude = model.Altitude;
            }

            LocationEdit.Hide();
            await SaveAsync();
            StateHasChanged();
        }

        protected void OnLocationSelected(int index)
        {
            LocationEdit.Show(Model.Location);
        }

        private async Task SaveAsync()
        {
            if (original.Equals(Model))
                return;

            await Api.UpdateVideoAsync(EntryId, Model);
            UpdateOriginal();
            StateHasChanged();
        }

        private void UpdateOriginal() => original = Model.Clone();

        protected async Task DeleteAsync()
        {
            if (await Navigator.AskAsync($"Do you really want to delete this video?"))
            {
                await Api.DeleteVideoAsync(EntryId, VideoId);
                Navigator.OpenEntryDetail(EntryId);
            }
        }

        protected async Task SetLocationOriginalAsync()
        {
            await Api.SetVideoLocationFromOriginalAsync(EntryId, VideoId);
            await LoadAsync();
        }

        protected async Task DownloadOriginalAsync()
        {
            var stream = await Api.GetMediaDataAsync(Model.Original.Url);
            Log.Debug($"Original downloaded, size '{stream.Length}'.");
            await Downloader.FromStreamAsync(Model.Name, stream, Model.ContentType);
            Log.Debug($"JS interop completed.");
        }

        protected Task OnClearLocationAsync() 
        {
            Markers[0].Latitude = null;
            Markers[0].Longitude = null;
            Markers[0].Altitude = null;
            return SaveLocationAsync();
        }

        protected string GetMapDescription(bool isVisible)
        {
            const string addLocationText = "No Location on Map...";
            const string noLocationText = "No location...";

            if (isVisible)
            {
                if (Model.Location.HasValue())
                    return Model.Location.ToRoundedString();
                else if (Permissions.IsEditable)
                    return addLocationText;
                else
                    return noLocationText;
            }
            else
            {
                if (Model.Location.HasValue())
                    return "Show Location on Map";
                else if (Permissions.IsEditable)
                    return addLocationText;
                else
                    return noLocationText;
            }
        }
    }
}
