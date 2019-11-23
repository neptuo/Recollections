using Microsoft.AspNetCore.Components;
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
    public class StoryPickerModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Parameter]
        public Action<EntryStoryModel> Selected { get; set; }

        protected ModalModel Modal { get; set; }

        private bool isFirstShow = true;

        protected List<StoryListModel> Stories { get; } = new List<StoryListModel>();
        protected Dictionary<string, List<StoryChapterListModel>> Chapters = new Dictionary<string, List<StoryChapterListModel>>();

        private async Task LoadAsync()
        {
            Stories.Clear();
            Stories.AddRange(await Api.GetStoryListAsync());
            StateHasChanged();
        }

        protected void Select(StoryListModel story, StoryChapterListModel chapter)
        {
            Hide();

            if (Selected != null)
            {
                Selected(new EntryStoryModel()
                {
                    StoryId = story?.Id,
                    StoryTitle = story?.Title,
                    ChapterId = chapter?.Id,
                    ChapterTitle = chapter?.Title
                });
            }
        }

        protected async Task LoadChaptersAsync(StoryListModel story)
        {
            if (story.Chapters > 0 && !Chapters.ContainsKey(story.Id))
                Chapters[story.Id] = await Api.GetStoryChapterListAsync(story.Id);
        }

        public void Show()
        {
            Modal.Show();

            if (isFirstShow)
            {
                isFirstShow = false;
                _ = LoadAsync();
            }
        }

        public void Hide() => Modal.Hide();
    }
}
