using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Entries.Components;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class BeingDetail : UserStateComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        private string previousBeingId;

        [Parameter]
        public string BeingId { get; set; }

        protected EntryPicker EntryPicker { get; set; }
        protected BeingModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();

        protected BeingIconPicker IconPicker { get; set; }

        protected List<MapEntryModel> MapEntries { get; set; } = new List<MapEntryModel>();
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        protected int StoriesCount { get; set; }
        protected Offcanvas StoriesOffcanvas { get; set; }
        protected bool IsStoriesLoading { get; set; }
        protected List<StoryListModel> StoryItems { get; } = new List<StoryListModel>();

        protected int AltitudeCount { get; set; }
        protected Offcanvas AltitudeOffcanvas { get; set; }
        protected bool IsAltitudeLoading { get; set; }
        protected List<EntryListModel> AltitudeItems { get; } = new List<EntryListModel>();

        public override Task SetParametersAsync(ParameterView parameters)
        {
            previousBeingId = BeingId;
            return base.SetParametersAsync(parameters);
        }

        protected async override Task OnParametersSetAsync()
        {
            if (previousBeingId != BeingId)
                await LoadAsync();
        }

        protected async Task LoadAsync()
        {
            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetBeingAsync(BeingId);

            Permissions.IsEditable = UserState.IsEditable && userPermission == Permission.CoOwner;
            Permissions.IsOwner = UserState.UserId == Model.UserId;

            await LoadMapAsync();

            var stories = await Api.GetBeingStoriesAsync(BeingId);
            StoriesCount = stories.Count;

            var altitudeEntries = await Api.GetBeingHighestAltitudeAsync(BeingId);
            AltitudeCount = altitudeEntries.Count;
        }

        private async Task LoadMapAsync()
        {
            MapEntries = await Api.GetBeingMapAsync(BeingId);
            Markers.Clear();
            foreach (var entry in MapEntries)
            {
                Markers.Add(new MapMarkerModel()
                {
                    Latitude = entry.Location.Latitude,
                    Longitude = entry.Location.Longitude,
                    Altitude = entry.Location.Altitude,
                    Title = entry.Title
                });
            }
        }

        protected void OnMarkerSelected(int index)
        {
            var entry = MapEntries[index];
            Navigator.OpenEntryDetail(entry.Id);
        }

        protected async Task SaveAsync()
        {
            await Api.UpdateBeingAsync(Model);
            StateHasChanged();
        }

        protected Task SaveNameAsync(string title)
        {
            Model.Name = title;
            return SaveAsync();
        }

        protected Task SaveIconAsync(string icon)
        {
            Model.Icon = icon;
            return SaveAsync();
        }

        protected Task SaveTextAsync(string text)
        {
            Model.Text = text;
            return SaveAsync();
        }

        protected async Task DeleteAsync()
        {
            if (await Navigator.AskAsync($"Do you really want to delete being '{Model.Name}'?"))
            {
                await Api.DeleteBeingAsync(Model.Id);
                Navigator.OpenBeings();
            }
        }

        protected async Task ShowStoriesAsync()
        {
            IsStoriesLoading = true;
            StoryItems.Clear();
            StoriesOffcanvas.Show();
            StateHasChanged();

            StoryItems.AddRange(await Api.GetBeingStoriesAsync(BeingId));
            IsStoriesLoading = false;
            StateHasChanged();
        }

        protected async Task ShowAltitudeAsync()
        {
            IsAltitudeLoading = true;
            AltitudeItems.Clear();
            AltitudeOffcanvas.Show();
            StateHasChanged();

            AltitudeItems.AddRange(await Api.GetBeingHighestAltitudeAsync(BeingId));
            IsAltitudeLoading = false;
            StateHasChanged();
        }

        protected string FormatAltitudeEntryTitle(EntryListModel entry)
            => UiOptions.FormatAltitudeEntryTitle(entry);
    }
}
