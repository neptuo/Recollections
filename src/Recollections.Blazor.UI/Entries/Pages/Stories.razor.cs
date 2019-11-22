using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Entries.Stories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public class StoriesModel : ComponentBase
    {
        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        public string Title { get; set; }
        public List<string> ErrorMessages { get; } = new List<string>();

        public List<StoryListModel> Stories { get; } = new List<StoryListModel>();

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            Stories.Add(new StoryListModel()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "My second story",
                MinDate = DateTime.Today.AddDays(-3),
                MaxDate = DateTime.Today,
                Entries = 4,
                Chapters = 2
            });

            Stories.Add(new StoryListModel()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "My first story",
                MinDate = DateTime.Today.AddDays(-7),
                MaxDate = DateTime.Today.AddDays(-12),
                Entries = 9,
                Chapters = 6
            });
        }

        protected Task CreateAsync()
        {
            Stories.Add(new StoryListModel()
            {
                Id = Guid.NewGuid().ToString(),
                Title = Title
            });
            Title = null;

            return Task.CompletedTask;
        }
    }
}
