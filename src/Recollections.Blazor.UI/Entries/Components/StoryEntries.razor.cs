using Microsoft.AspNetCore.Components;
using Neptuo.Events;
using Neptuo.Events.Handlers;
using Neptuo.Recollections.Entries.Events;
using Neptuo.Recollections.Entries.Stories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class StoryEntriesModel : ComponentBase, IDisposable, IEventHandler<StoryEntriesChanged>
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected IEventHandlerCollection EventHandlers { get; set; }

        [Parameter]
        public string StoryId { get; set; }

        [Parameter]
        public string ChapterId { get; set; }

        protected List<StoryEntryModel> Models { get; } = new List<StoryEntryModel>();

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            Models.Clear();
            if (StoryId != null)
            {
                if (ChapterId == null)
                    Models.AddRange(await Api.GetStoryEntryListAsync(StoryId));
                else
                    Models.AddRange(await Api.GetStoryChapterEntryListAsync(StoryId, ChapterId));
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            EventHandlers.Add<StoryEntriesChanged>(this);
        }

        public void Dispose()
        {
            EventHandlers.Remove<StoryEntriesChanged>(this);
        }

        async Task IEventHandler<StoryEntriesChanged>.HandleAsync(StoryEntriesChanged payload)
        {
            await LoadDataAsync();
            StateHasChanged();
        }
    }
}
