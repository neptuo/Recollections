using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Components;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages
{
    public partial class Profile
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Entries.Api EntriesApi { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Parameter]
        public string UserId { get; set; }

        protected ProfileModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();

        protected List<MapEntryModel> MapEntries { get; set; } = new List<MapEntryModel>();
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        protected int StoriesCount { get; set; }
        protected Offcanvas StoriesOffcanvas { get; set; }
        protected bool IsStoriesLoading { get; set; }
        protected List<StoryListModel> StoryItems { get; } = new List<StoryListModel>();

        protected Offcanvas AltitudeOffcanvas { get; set; }
        protected List<EntryListModel> AltitudeItems { get; } = new List<EntryListModel>();

        public async override Task SetParametersAsync(ParameterView parameters)
        {
            var oldUserId = UserId;
            await base.SetParametersAsync(parameters);

            if (oldUserId != UserId)
                await LoadAsync();
        }

        protected async Task LoadAsync()
        {
            Model = null;
            Owner = null;

            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetProfileAsync(UserId);

            Permissions.IsEditable = UserState.IsEditable && userPermission == Permission.CoOwner;
            Permissions.IsOwner = UserState.UserId == UserId;

            await LoadMapAsync();

            var stories = await EntriesApi.GetProfileStoriesAsync(UserId);
            StoriesCount = stories.Count;

            AltitudeItems.Clear();
            AltitudeItems.AddRange(await EntriesApi.GetProfileHighestAltitudeAsync(UserId));

            StateHasChanged();
        }

        private async Task LoadMapAsync()
        {
            MapEntries = await EntriesApi.GetProfileMapAsync(UserId);
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

        protected string FormatAltitudeTitle(EntryListModel entry)
            => entry.Altitude != null
                ? $"{UiOptions.FormatWholeNumber(entry.Altitude.Value)} m"
                : entry.When.ToString(UiOptions.ShortDateFormat);

        protected async Task ShowStoriesAsync()
        {
            IsStoriesLoading = true;
            StoryItems.Clear();
            StoriesOffcanvas.Show();
            StateHasChanged();

            StoryItems.AddRange(await EntriesApi.GetProfileStoriesAsync(UserId));
            IsStoriesLoading = false;
            StateHasChanged();
        }

        protected void ShowAltitudeAsync()
        {
            AltitudeOffcanvas.Show();
        }
    }
}
