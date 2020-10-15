using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Entries.Stories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class Stories
    {
        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        protected bool IsLoading { get; set; }

        public string Title { get; set; }
        public List<string> ErrorMessages { get; } = new List<string>();

        public List<StoryListModel> Items { get; } = new List<StoryListModel>();

        protected async override Task OnInitializedAsync()
        {
            IsLoading = true;

            await base.OnInitializedAsync();
            await UserState.EnsureAuthenticatedAsync();

            await LoadDataAsync();
        }

        protected async Task LoadDataAsync()
        {
            IsLoading = true;
            Items.Clear();
            Items.AddRange(await Api.GetStoryListAsync());
            IsLoading = false;
        }

        protected async Task CreateAsync()
        {
            var model = new StoryModel()
            {
                Id = Guid.NewGuid().ToString(),
                Title = Title
            };

            await Api.CreateStoryAsync(model);
            Navigator.OpenStoryDetail(model.Id);
        }
    }
}
