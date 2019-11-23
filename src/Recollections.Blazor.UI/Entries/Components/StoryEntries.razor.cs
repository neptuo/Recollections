using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Entries.Stories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class StoryEntriesModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Parameter]
        public string StoryId { get; set; }

        [Parameter]
        public string ChapterId { get; set; }

        protected List<StoryEntryModel> Models { get; } = new List<StoryEntryModel>();

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            Models.Clear();
            if (StoryId != null)
            {
                if (ChapterId == null)
                    Models.AddRange(await Api.GetStoryEntryListAsync(StoryId));
                else
                    Models.AddRange(await Api.GetStoryChapterEntryListAsync(StoryId, ChapterId));
            }
        }
    }
}
