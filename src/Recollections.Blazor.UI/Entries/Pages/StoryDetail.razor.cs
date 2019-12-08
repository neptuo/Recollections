using Microsoft.AspNetCore.Components;
using Neptuo.Events;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Entries.Components;
using Neptuo.Recollections.Entries.Events;
using Neptuo.Recollections.Entries.Stories;
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
        protected IEventDispatcher EventDispatcher { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        [Parameter]
        public string Id { get; set; }

        protected EntryPicker EntryPicker { get; set; }
        protected StoryModel Model { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await UserState.EnsureAuthenticatedAsync();

            await LoadAsync();
        }

        protected async Task LoadAsync()
        {
            Model = await Api.GetStoryAsync(Id);
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
            Model.Chapters.Add(new ChapterModel()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "New Chapter"
            });
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
            await Api.UpdateEntryStoryAsync(entry.Id, model);
            await EventDispatcher.PublishAsync(new StoryEntriesChanged(model.StoryId, model.ChapterId, entry.Id));
        }
    }
}
