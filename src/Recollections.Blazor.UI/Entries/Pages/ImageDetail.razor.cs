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
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

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

        protected Task DownloadOriginalAsync() => Downloader.FromUrlAsync(Model.Name, Api.ResolveUrl(Model.Original));

        protected string GetMapDescription(bool isVisible)
        {
            if (isVisible)
            {
                if (Model.Location.HasValue())
                    return Model.Location.ToString();
                else
                    return "Add Location on Map";
            }
            else
            {
                if (Model.Location.HasValue())
                    return "Show Location on Map";
                else
                    return "Add Location on Map";
            }
        }
    }
}
