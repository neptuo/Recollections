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
    public class StoryDetailModel : ComponentBase
    {
        [Parameter]
        public string Id { get; set; }

        protected StoryModel Model { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            Model = new StoryModel()
            {
                Id = Id,
                Title = "My First Story",
            };
        }

        protected Task SaveAsync()
        {
            return Task.CompletedTask;
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

        protected Task DeleteAsync()
        {
            return Task.CompletedTask;
        }
    }
}
