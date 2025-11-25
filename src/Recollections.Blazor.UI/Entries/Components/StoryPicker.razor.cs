using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Stories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class StoryPicker(ILog<StoryPicker> log)
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Parameter]
        public EventCallback<EntryStoryModel> Selected { get; set; }

        [CascadingParameter]
        public UserState UserState { get; set; }

        protected Modal Modal { get; set; }

        private bool isFirstShow = true;

        protected bool IsLoading { get; set; }
        protected List<StoryListModel> AllStories { get; } = [];
        protected List<StoryListModel> Stories { get; } = [];
        protected string ErrorMessage { get; set; }
        private Dictionary<string, StoryState> StoryStates { get; set; } = new();

        protected string SelectedStoryId;
        protected string SelectedChapterId;

        protected string SearchQuery { get; set; }

        private async Task LoadAsync()
        {
            IsLoading = true;
            AllStories.Clear();
            AllStories.AddRange(await Api.GetStoryListAsync());
            OnSearch();
            IsLoading = false;

            StateHasChanged();
        }

        protected async Task SelectAsync(StoryListModel story, StoryChapterListModel chapter)
        {
            ErrorMessage = null;
            if (Selected.HasDelegate)
            {
                await Selected.InvokeAsync(new EntryStoryModel()
                {
                    StoryId = story?.Id,
                    StoryTitle = story?.Title,
                    ChapterId = chapter?.Id,
                    ChapterTitle = chapter?.Title
                });
            }

            if (ErrorMessage == null)
                Hide();
        }

        public void SetErrorMessage(string errorMessage = null)
            => ErrorMessage = errorMessage;

        protected async Task LoadChaptersAsync(StoryListModel story)
        {
            if (story.Chapters > 0)
            {
                if (!StoryStates.TryGetValue(story.Id, out var state))
                    StoryStates[story.Id] = state = new ();

                if (state.Chapters == null)
                {
                    try
                    {
                        state.IsLoading = true;
                        state.Chapters = await Api.GetStoryChapterListAsync(story.Id);
                    }
                    finally
                    {
                        state.IsLoading = false;
                    }
                }

                state.IsExpanded = !state.IsExpanded;
            }
        }

        protected void OnSearch()
        {
            log.Debug($"OnSearch '{SearchQuery}'");

            Stories.Clear();
            if (String.IsNullOrEmpty(SearchQuery))
            {
                Stories.AddRange(AllStories);
                return;
            }

            string searchQuery = SearchQuery.ToLower().Trim();
            Stories.AddRange(AllStories.Where(m => m.Title.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)));
        }

        public async void Show(string storyId = null, string chapterId = null)
        {
            SelectedStoryId = storyId;
            SelectedChapterId = chapterId;
            ErrorMessage = null;
            StoryStates.Clear();
            SearchQuery = null;

            Modal.Show();

            if (isFirstShow)
            {
                isFirstShow = false;
                await LoadAsync();
            }
            else
            {
                OnSearch();
                StateHasChanged();
            }

            if (chapterId != null)
            {
                var story = AllStories.FirstOrDefault(s => s.Id == storyId);
                if (story != null)
                {
                    await LoadChaptersAsync(story);
                    StateHasChanged();
                }
            }
        }

        public void Hide() => Modal.Hide();
    }
    
    class StoryState
    {
        public bool IsLoading { get; set; }
        public bool IsExpanded { get; set; }
        public List<StoryChapterListModel> Chapters { get; set; }
    }
}
