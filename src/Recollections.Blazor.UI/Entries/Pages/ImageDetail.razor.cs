using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class ImageDetail
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
        protected ILog<ImageDetail> Log { get; set; }

        [Parameter]
        public string EntryId { get; set; }

        private string previousImageId;

        [Parameter]
        public string ImageId { get; set; }

        private ImageModel original;
        protected ImageModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();

        public override Task SetParametersAsync(ParameterView parameters)
        {
            previousImageId = ImageId;
            return base.SetParametersAsync(parameters);
        }

        protected async override Task OnParametersSetAsync()
        {
            if (previousImageId != ImageId)
                await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetImageAsync(EntryId, ImageId);

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

        protected async Task SaveLocationAsync()
        {
            Model.Location.Latitude = Markers[0].Latitude;
            Model.Location.Longitude = Markers[0].Longitude;
            Model.Location.Altitude = Markers[0].Altitude;

            await SaveAsync();
            StateHasChanged();
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

        protected async Task DownloadOriginalAsync()
        {
            var stream = await Api.GetImageDataAsync(Model.Original.Url);
            Log.Debug($"Original downloaded, size '{stream.Length}'.");
            await Downloader.FromStreamAsync(Model.Name, stream, "image/png");
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
            const string addLocationText = "Add Location on Map";
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
