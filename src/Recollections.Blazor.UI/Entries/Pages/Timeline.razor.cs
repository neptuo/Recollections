using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class Timeline
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected MarkdownConverter MarkdownConverter { get; set; }

        [Inject]
        protected ILog<Timeline> Log { get; set; }

        [Parameter]
        public bool AllowCreate { get; set; } = true;

        [Parameter]
        public bool AllowMore { get; set; } = true;

        [Parameter]
        public string UserId { get; set; }

        [Parameter]
        public Func<int, Task<TimelineListResponse>> DataGetter { get; set; }

        private int offset;

        protected List<TimelineEntryModel> Entries { get; } = new List<TimelineEntryModel>();
        protected bool HasMore { get; private set; }
        protected bool IsLoading { get; private set; } = true;

        protected async override Task OnInitializedAsync()
        {
            Log.Debug("Timeline.Init");

            await base.OnInitializedAsync();

            Log.Debug("Timeline.Load");
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                IsLoading = true;

                TimelineListResponse response = DataGetter != null
                    ? await DataGetter(offset)
                    : UserId == null
                        ? await Api.GetTimelineListAsync(offset)
                        : await Api.GetTimelineListAsync(UserId, offset);

                Entries.AddRange(response.Entries);
                HasMore = response.HasMore;
                offset = Entries.Count;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public Task LoadMoreAsync()
            => LoadAsync();
    }
}
