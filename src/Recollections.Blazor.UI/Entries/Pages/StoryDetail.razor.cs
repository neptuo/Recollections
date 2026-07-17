using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Neptuo.Exceptions.Handlers;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Components.Editors;
using Neptuo.Recollections.Entries.Components;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class StoryDetail(ILog<StoryDetail> log) : IAsyncDisposable
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected IExceptionHandler ExceptionHandler { get; set; }

        private string previousStoryId;
        private long loadVersion;
        private bool shouldLoadTimelineData;
        private bool shouldLoadSecondaryData;
        private string[] timelineChapterIds = [];

        [Parameter]
        public string StoryId { get; set; }

        protected EntryPicker EntryPicker { get; set; }
        protected Gallery Gallery { get; set; }
        protected StoryModel Model { get; set; }
        protected Dictionary<string, List<EntryListModel>> Entries { get; set; } = [];
        protected OwnerModel Owner { get; set; }
        protected PermissionContainerState Permissions { get; } = new();
        protected List<EntryMediaModel> Media { get; set; } = [];
        protected List<MediaModel> AllMedia { get; set; } = [];
        protected List<GalleryModel> GalleryItems { get; } = [];
        protected bool IsTimelineLoading { get; set; }
        protected bool IsMapLoading { get; set; }
        protected bool IsMediaLoading { get; set; }
        protected bool SelectLastChapterTitleEdit { get; set; }
        protected InlineTextEdit LastChapterTitleEdit { get; set; }
        protected GalleryPreviewModal GalleryPreviewModal { get; set; }
        
        protected MapPopoverHandler PopoverHandler { get; } = new();
        protected Map mapComponent;
        protected EntryCardPopover entryPopover;
        protected List<MapEntryModel> MapEntries { get; set; } = new List<MapEntryModel>();
        protected List<MapMarkerModel> Markers { get; } = new List<MapMarkerModel>();

        public override Task SetParametersAsync(ParameterView parameters)
        {
            previousStoryId = StoryId;
            return base.SetParametersAsync(parameters);
        }

        protected async override Task OnParametersSetAsync()
        {
            if (previousStoryId != StoryId)
                await LoadAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await PopoverHandler.TryShowPopoverAsync(mapComponent, entryPopover);

            if (shouldLoadTimelineData)
            {
                shouldLoadTimelineData = false;
                await LoadTimelineDataAsync(loadVersion, StoryId, timelineChapterIds);
            }

            if (shouldLoadSecondaryData)
            {
                shouldLoadSecondaryData = false;
                await LoadSecondaryDataAsync(loadVersion, StoryId);
            }

            if (SelectLastChapterTitleEdit)
            {
                log.Debug("Selecting last chapter title edit.");
                SelectLastChapterTitleEdit = false;
                await LastChapterTitleEdit.EditAsync();
            }
        }

        protected async Task LoadAsync()
        {
            long currentLoadVersion = Interlocked.Increment(ref loadVersion);

            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetStoryAsync(StoryId);

            // A newer story load has started while this one was fetching.
            if (currentLoadVersion != loadVersion)
                return;

            Permissions.IsEditable = UserState.IsEditable && userPermission == Permission.CoOwner;
            Permissions.IsOwner = UserState.UserId == Model.UserId;
            timelineChapterIds = Model.Chapters.Select(c => c.Id).ToArray();
            Entries.Clear();
            Entries[Model.Id] = [];
            foreach (var chapter in Model.Chapters)
                Entries[chapter.Id] = [];

            IsTimelineLoading = true;
            IsMapLoading = true;
            IsMediaLoading = true;
            ApplyMap([]);
            ApplyMedia([]);
            shouldLoadTimelineData = true;
            shouldLoadSecondaryData = true;
            StateHasChanged();
        }

        protected async Task LoadMediaAsync()
        {
            IsMediaLoading = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var media = await Api.GetStoryMediaAsync(StoryId);
                ApplyMedia(media);
            }
            finally
            {
                IsMediaLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private void ApplyMedia(List<EntryMediaModel> media)
        {
            Media = media;
            GalleryItems.Clear();
            AllMedia.Clear();
            foreach (var entry in Media)
            {
                foreach (var item in entry.Media)
                {
                    AllMedia.Add(item);
                    if (GalleryModelMapper.TryMap(item, Api, out GalleryModel galleryModel))
                        GalleryItems.Add(galleryModel);
                }
            }
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

        private async Task LoadSecondaryDataAsync(long currentLoadVersion, string storyId)
        {
            try
            {
                var mapTask = Api.GetStoryMapAsync(storyId);
                var mediaTask = Api.GetStoryMediaAsync(storyId);
                await Task.WhenAll(mapTask, mediaTask);

                if (currentLoadVersion != loadVersion || storyId != StoryId)
                    return;

                ApplyMap(mapTask.Result);
                ApplyMedia(mediaTask.Result);
            }
            finally
            {
                if (currentLoadVersion == loadVersion && storyId == StoryId)
                {
                    IsMapLoading = false;
                    IsMediaLoading = false;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private async Task LoadTimelineDataAsync(long currentLoadVersion, string storyId, string[] chapterIds)
        {
            var entriesTasks = new Task<PageableList<EntryListModel>>[chapterIds.Length + 1];
            entriesTasks[0] = Api.GetStoryTimelineAsync(storyId);
            for (int i = 0; i < chapterIds.Length; i++)
                entriesTasks[i + 1] = Api.GetStoryChapterTimelineAsync(storyId, chapterIds[i]);

            var entries = await Task.WhenAll(entriesTasks);
            if (currentLoadVersion != loadVersion || storyId != StoryId)
                return;

            Entries[storyId] = entries[0].Models;
            for (int i = 0; i < chapterIds.Length; i++)
                Entries[chapterIds[i]] = entries[i + 1].Models;

            IsTimelineLoading = false;
            StateHasChanged();
        }

        protected async Task OnMarkerSelectedAsync(int index)
        {
            await PopoverHandler.SelectAsync(index, MapEntries[index].Entry, entryPopover);
            StateHasChanged();
        }

        public async ValueTask DisposeAsync()
        {
            await PopoverHandler.DisposeAsync(entryPopover);
        }

        protected async Task SaveAsync()
        {
            await Api.UpdateStoryAsync(Model);
            StateHasChanged();
        }

        protected Task SaveTitleAsync(string title)
        {
            Model.Title = title;
            return SaveAsync();
        }

        protected Task SaveTextAsync(string text)
        {
            Model.Text = text;
            return SaveAsync();
        }

        protected async Task DeleteAsync()
        {
            if (await Navigator.AskAsync($"Do you really want to delete story '{Model.Title}'?"))
            {
                await Api.DeleteStoryAsync(Model.Id);
                Navigator.OpenStories();
            }
        }

        protected void AddChapter()
        {
            var chapter = new ChapterModel()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "New Chapter"
            };
            Model.Chapters.Add(chapter);
            Entries[chapter.Id] = new();
            SelectLastChapterTitleEdit = true;
        }

        protected Task SaveChapterTitleAsync(ChapterModel chapter, string title)
        {
            chapter.Title = title;
            return SaveAsync();
        }

        protected Task SaveChapterTextAsync(ChapterModel chapter, string text)
        {
            chapter.Text = text;
            return SaveAsync();
        }

        protected Task DeleteChapterAsync(ChapterModel chapter)
        {
            Model.Chapters.Remove(chapter);
            return SaveAsync();
        }

        private bool TryFindMedia(int index, out string entryId, out MediaModel media)
        {
            int i = 0;
            foreach (var entry in Media)
            {
                foreach (var item in entry.Media)
                {
                    if (index == i)
                    {
                        media = item;
                        entryId = entry.EntryId;
                        return true;
                    }

                    i++;
                }
            }

            entryId = null;
            media = null;
            return false;
        }

        protected async Task OnGalleryOpenInfoAsync(int index)
        {
            if (!TryFindMedia(index, out var entryId, out var item))
                return;

            await Gallery.CloseAsync();
            
            if (item.Image != null)
                Navigator.OpenImageDetail(entryId, item.Image.Id);
            else if (item.Video != null)
                Navigator.OpenVideoDetail(entryId, item.Video.Id);
        }

        protected async Task OnBeforeInternalNavigation(LocationChangingContext context)
        {
            if (await Gallery.IsOpenAsync())
            {
                _ = Gallery.CloseAsync();
                context.PreventNavigation();
            }
        }

        private ChapterModel entrySelectionChapter;

        protected void SelectEntry(ChapterModel chapter)
        {
            entrySelectionChapter = chapter;
            EntryPicker.Show();
        }

        protected async void EntrySelected(EntryListModel entry)
        {
            var model = new EntryStoryUpdateModel(Model.Id);

            if (entrySelectionChapter != null)
                model.ChapterId = entrySelectionChapter.Id;

            entrySelectionChapter = null;
            if (await Api.UpdateEntryStoryAsync(entry.Id, model))
            {
                string previousId = Entries.Where(s => s.Value.Any(e => e.Id == entry.Id)).Select(s => s.Key).FirstOrDefault();
                if (previousId == Model.Id)
                    Entries[Model.Id] = (await Api.GetStoryTimelineAsync(Model.Id)).Models;
                else if (previousId != null)
                    Entries[previousId] = (await Api.GetStoryChapterTimelineAsync(Model.Id, previousId)).Models;

                if (model.ChapterId != null)
                    Entries[model.ChapterId] = (await Api.GetStoryChapterTimelineAsync(Model.Id, model.ChapterId)).Models;
                else
                    Entries[Model.Id] = (await Api.GetStoryTimelineAsync(Model.Id)).Models;
            }
            else
            {
                ExceptionHandler.Handle(new Exception("Missing required co-owner permission to select the entry"));
            }

            await LoadMediaAsync();
            StateHasChanged();
        }
    }
}
