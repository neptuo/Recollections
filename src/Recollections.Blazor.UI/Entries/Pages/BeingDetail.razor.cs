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
    public partial class BeingDetail : UserStateComponentBase, IAsyncDisposable
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        private string previousBeingId;
        private readonly VersionedDeferredSecondaryLoadState<string> loadState = new();
        private Task secondaryDataTask;

        [Parameter]
        public string BeingId { get; set; }

        protected BeingModel Model { get; set; }
        protected OwnerModel Owner { get; set; }
        protected PermissionContainerState Permissions { get; } = new PermissionContainerState();

        protected BeingIconPicker IconPicker { get; set; }

        protected MapPopoverHandler PopoverHandler { get; } = new();
        protected Map mapComponent;
        protected EntryCardPopover entryPopover;
        protected List<MapEntryModel> MapEntries { get; set; } = new List<MapEntryModel>();
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        protected bool IsMapLoading { get; set; }
        protected int StoriesCount { get; set; }
        protected Offcanvas StoriesOffcanvas { get; set; }
        protected bool IsStoriesLoading { get; set; }
        protected List<StoryListModel> StoryItems { get; } = new List<StoryListModel>();

        protected int AltitudeCount { get; set; }
        protected double? HighestAltitude { get; set; }
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
            long currentLoadVersion = loadState.BeginLoad();

            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetBeingAsync(BeingId);

            if (!loadState.IsCurrent(currentLoadVersion))
                return;

            Permissions.IsEditable = UserState.IsEditable && userPermission == Permission.CoOwner;
            Permissions.IsOwner = UserState.UserId == Model.UserId;

            IsMapLoading = true;
            IsStoriesLoading = true;
            IsAltitudeLoading = true;
            ApplyMap([]);
            StoryItems.Clear();
            AltitudeItems.Clear();
            StoriesCount = 0;
            AltitudeCount = 0;
            HighestAltitude = null;
            loadState.ScheduleSecondaryLoad(BeingId);
            StateHasChanged();
        }

        private void ApplyMap(List<MapEntryModel> mapEntries)
        {
            MapEntries = mapEntries;
            Markers.Clear();
            foreach (var entry in MapEntries)
            {
                Markers.Add(new MapMarkerModel()
                {
                    Latitude = entry.Location.Latitude,
                    Longitude = entry.Location.Longitude,
                    Altitude = entry.Location.Altitude,
                    Title = entry.Entry.Title
                });
            }
        }

        private async Task LoadSecondaryDataAsync(long currentLoadVersion, string beingId)
        {
            try
            {
                var mapTask = Api.GetBeingMapAsync(beingId);
                var storiesTask = Api.GetBeingStoriesAsync(beingId);
                var altitudeTask = Api.GetBeingHighestAltitudeAsync(beingId);
                await Task.WhenAll(mapTask, storiesTask, altitudeTask);

                if (!loadState.IsCurrent(currentLoadVersion, beingId))
                    return;

                ApplyMap(mapTask.Result);
                StoryItems.AddRange(storiesTask.Result);
                StoriesCount = StoryItems.Count;
                AltitudeItems.AddRange(altitudeTask.Result);
                AltitudeCount = AltitudeItems.Count;
                HighestAltitude = AltitudeItems.FirstOrDefault()?.Altitude;
            }
            finally
            {
                if (loadState.IsCurrent(currentLoadVersion, beingId))
                {
                    IsMapLoading = false;
                    IsStoriesLoading = false;
                    IsAltitudeLoading = false;
                    StateHasChanged();
                }
            }
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

            if (loadState.TryConsumeSecondaryLoad(out long currentLoadVersion, out string beingId))
            {
                secondaryDataTask = LoadSecondaryDataAsync(currentLoadVersion, beingId);
                await secondaryDataTask;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await PopoverHandler.DisposeAsync(entryPopover);
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

        protected Task SaveBirthDateAsync(DateTime birthDate)
        {
            Model.BirthDate = birthDate == DateTime.MinValue ? null : birthDate.Date;
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
            if (IsStoriesLoading && secondaryDataTask != null)
                await secondaryDataTask;

            StoriesOffcanvas.Show();
            StateHasChanged();
        }

        protected async Task ShowAltitudeAsync()
        {
            if (IsAltitudeLoading && secondaryDataTask != null)
                await secondaryDataTask;

            AltitudeOffcanvas.Show();
            StateHasChanged();
        }

        protected string FormatAltitudeEntryTitle(EntryListModel entry)
            => UiOptions.FormatAltitudeEntryTitle(entry);
    }
}
