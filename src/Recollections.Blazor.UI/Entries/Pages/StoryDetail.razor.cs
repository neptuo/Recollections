﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Neptuo.Exceptions.Handlers;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
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
    public partial class StoryDetail
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
        protected Dictionary<string, List<TimelineEntryModel>> Entries { get; set; } = new();
        protected OwnerModel Owner { get; set; }
        protected PermissionContainerState Permissions { get; } = new();
        protected List<EntryImagesModel> Images { get; set; }
        protected List<GalleryModel> GalleryItems { get; } = new List<GalleryModel>();

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

        protected async Task LoadAsync()
        {
            Permission userPermission;
            (Model, Owner, userPermission) = await Api.GetStoryAsync(StoryId);

            Permissions.IsEditable = UserState.IsEditable && userPermission == Permission.CoOwner;
            Permissions.IsOwner = UserState.UserId == Model.UserId;

            var entriesTasks = new Task<TimelineListResponse>[Model.Chapters.Count + 1];
            entriesTasks[0] = Api.GetStoryTimelineAsync(Model.Id);
            for (int i = 0; i < Model.Chapters.Count; i++)
                entriesTasks[i + 1] = Api.GetStoryChapterTimelineAsync(Model.Id, Model.Chapters[i].Id);

            var entries = await Task.WhenAll(entriesTasks);
            Entries[Model.Id] = entries[0].Entries;
            for (int i = 0; i < Model.Chapters.Count; i++)
                Entries[Model.Chapters[i].Id] = entries[i + 1].Entries;

            Images = await Api.GetStoryImagesAsync(StoryId);
            GalleryItems.Clear();
            foreach (var entry in Images)
            {
                foreach (var image in entry.Images)
                {
                    GalleryItems.Add(new GalleryModel()
                    {
                        Title = image.Name,
                        Width = image.Preview.Width,
                        Height = image.Preview.Height
                    });
                }
            }
        }

        protected Task SaveAsync()
            => Api.UpdateStoryAsync(Model);

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

        private bool TryFindImage(int index, out string entryId, out ImageModel image)
        {
            int i = 0;
            foreach (var entry in Images)
            {
                foreach (var item in entry.Images)
                {
                    if (index == i)
                    {
                        image = item;
                        entryId = entry.EntryId;
                        return true;
                    }

                    i++;
                }
            }

            entryId = null;
            image = null;
            return false;
        }

        protected async Task<Stream> OnGetImageDataAsync(int index)
        {
            if (!TryFindImage(index, out _, out var image))
                return null;

            var stream = await Api.GetImageDataAsync(image.Preview.Url);
            return stream;
        }

        protected async Task OpenImageDetailAsync(int index)
        {
            if (!TryFindImage(index, out var entryId, out var image))
                return;

            await Gallery.CloseAsync();
            Navigator.OpenImageDetail(entryId, image.Id);
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

        protected async void EntrySelected(TimelineEntryModel entry)
        {
            var model = new EntryStoryUpdateModel(Model.Id);

            if (entrySelectionChapter != null)
                model.ChapterId = entrySelectionChapter.Id;

            entrySelectionChapter = null;
            if (await Api.UpdateEntryStoryAsync(entry.Id, model))
            {
                string previousId = Entries.Where(s => s.Value.Any(e => e.Id == entry.Id)).Select(s => s.Key).FirstOrDefault();
                if (previousId == Model.Id)
                    Entries[Model.Id] = (await Api.GetStoryTimelineAsync(Model.Id)).Entries;
                else if (previousId != null)
                    Entries[previousId] = (await Api.GetStoryChapterTimelineAsync(Model.Id, previousId)).Entries;

                if (model.ChapterId != null)
                    Entries[model.ChapterId] = (await Api.GetStoryChapterTimelineAsync(Model.Id, model.ChapterId)).Entries;
                else
                    Entries[Model.Id] = (await Api.GetStoryTimelineAsync(Model.Id)).Entries;
            }
            else
            {
                ExceptionHandler.Handle(new Exception("Missing required co-owner permission to select the entry"));
            }

            StateHasChanged();
        }
    }
}
