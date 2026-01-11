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
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class StoryDetail(ILog<StoryDetail> log)
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected IExceptionHandler ExceptionHandler { get; set; }

        private string previousStoryId;

        [Parameter]
        public string StoryId { get; set; }

        protected EntryPicker EntryPicker { get; set; }
        protected Gallery Gallery { get; set; }
        protected StoryModel Model { get; set; }
        protected Dictionary<string, List<EntryListModel>> Entries { get; set; } = [];
        protected OwnerModel Owner { get; set; }
        protected PermissionContainerState Permissions { get; } = new();
        protected List<EntryMediaModel> Media { get; set; } = [];
        protected List<GalleryModel> GalleryItems { get; } = [];
        protected bool SelectLastChapterTitleEdit { get; set; }
        protected InlineTextEdit LastChapterTitleEdit { get; set; }
        protected Modal GalleryPreviewModal { get; set; }
        protected bool LoadGalleryPreviews { get; set; }
        
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

            if (SelectLastChapterTitleEdit)
            {
                log.Debug("Selecting last chapter title edit.");
                SelectLastChapterTitleEdit = false;
                await LastChapterTitleEdit.EditAsync();
            }
        }

        protected async Task LoadAsync()
        {
            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetStoryAsync(StoryId);

            Permissions.IsEditable = UserState.IsEditable && userPermission == Permission.CoOwner;
            Permissions.IsOwner = UserState.UserId == Model.UserId;

            var entriesTasks = new Task<PageableList<EntryListModel>>[Model.Chapters.Count + 1];
            entriesTasks[0] = Api.GetStoryTimelineAsync(Model.Id);
            for (int i = 0; i < Model.Chapters.Count; i++)
                entriesTasks[i + 1] = Api.GetStoryChapterTimelineAsync(Model.Id, Model.Chapters[i].Id);

            var entries = await Task.WhenAll(entriesTasks);
            Entries[Model.Id] = entries[0].Models;
            for (int i = 0; i < Model.Chapters.Count; i++)
                Entries[Model.Chapters[i].Id] = entries[i + 1].Models;

            await LoadMapAsync();
            await LoadMediaAsync();
        }

        protected async Task LoadMediaAsync()
        {
            Media.Clear();
            Media = await Api.GetStoryMediaAsync(StoryId);
            GalleryItems.Clear();
            foreach (var entry in Media)
            {
                foreach (var item in entry.Media)
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
        }

        private async Task LoadMapAsync()
        {
            MapEntries = await Api.GetStoryMapAsync(StoryId);
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

        protected Task<Stream> OnGetMediaDataAsync(int index, string type)
        {
            if (TryFindMedia(index, out _, out var item))
            {
                if (item.Image != null)
                {
                    return Api.GetMediaDataAsync(item.Image.Preview.Url);
                }
                else if (item.Video != null)
                {
                    if (type == "original")
                        return Api.GetMediaDataAsync(item.Video.Original.Url);
                    else
                        return Api.GetMediaDataAsync(item.Video.Preview.Url);
                }
            }

            return Task.FromResult<Stream>(null);
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
