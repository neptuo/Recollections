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
    public partial class Profile : IAsyncDisposable
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

        protected MapPopoverHandler PopoverHandler { get; } = new();
        protected Map mapComponent;
        protected EntryCardPopover entryPopover;
        protected List<MapEntryModel> MapEntries { get; set; } = new List<MapEntryModel>();
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        protected int StoriesCount { get; set; }
        protected Offcanvas StoriesOffcanvas { get; set; }
        protected bool IsStoriesLoading { get; set; }
        protected List<StoryListModel> StoryItems { get; } = new List<StoryListModel>();
        private List<StoryListModel> cachedStories;

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

            cachedStories = await EntriesApi.GetProfileStoriesAsync(UserId);
            StoriesCount = cachedStories.Count;

            AltitudeItems.Clear();
            AltitudeItems.AddRange(await EntriesApi.GetProfileHighestAltitudeAsync(UserId));

            StateHasChanged();
        }

        private async Task LoadMapAsync()
        {
            MapEntries = await EntriesApi.GetProfileMapAsync(UserId);
            Markers.Clear();
            Markers.AddRange(CreateMarkers(MapEntries));
        }

        private static List<MapMarkerModel> CreateMarkers(List<MapEntryModel> entries)
        {
            var markers = new List<MapMarkerModel>();
            foreach (var entry in entries)
            {
                markers.Add(new MapMarkerModel()
                {
                    Latitude = entry.Location.Latitude,
                    Longitude = entry.Location.Longitude,
                    Altitude = entry.Location.Altitude,
                    Title = entry.Entry.Title
                });
            }
            return markers;
        }

        protected async Task OnMarkerSelectedAsync(int index)
        {
            await PopoverHandler.SelectAsync(index, MapEntries[index].Entry, entryPopover);
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await PopoverHandler.TryShowPopoverAsync(mapComponent, entryPopover);
        }

        public async ValueTask DisposeAsync()
        {
            await PopoverHandler.DisposeAsync(entryPopover);
        }

        protected string FormatAltitudeTitle(EntryListModel entry)
            => UiOptions.FormatAltitudeEntryTitle(entry);

        protected Task ShowStoriesAsync()
        {
            IsStoriesLoading = true;
            StoryItems.Clear();
            StoriesOffcanvas.Show();
            StateHasChanged();

            try
            {
                StoryItems.AddRange(cachedStories);
            }
            finally
            {
                IsStoriesLoading = false;
                StateHasChanged();
            }

            return Task.CompletedTask;
        }

        protected void ShowAltitudeAsync()
        {
            AltitudeOffcanvas.Show();
        }
    }
}
