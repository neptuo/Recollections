using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
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
        protected ILog<Timeline> Log { get; set; }

        [Parameter]
        public RenderFragment BeforeContent { get; set; }

        [Parameter]
        public string NoMoreText { get; set; }

        [Parameter]
        public bool AllowMore { get; set; } = true;

        [Parameter]
        public bool ShowStoryInfo { get; set; } = true;

        [Parameter]
        public bool ShowYearSeparators { get; set; } = false;

        [Parameter]
        public List<TimelineEntryModel> Data { get; set; }

        [Parameter]
        public Func<int, Task<TimelineListResponse>> DataGetter { get; set; }

        private int offset;
        private Task loadAsyncFromParametersSet;

        protected List<TimelineEntryModel> Entries { get; } = new List<TimelineEntryModel>();
        protected bool HasMore { get; private set; }
        protected bool IsLoading { get; private set; } = true;

        protected async override Task OnInitializedAsync()
        {
            Log.Debug("Init");

            await base.OnInitializedAsync();
        }

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (Data != null)
            {
                AllowMore = false;
                Entries.Clear();
                Entries.AddRange(Data);
                Log.Debug($"Got parameter Data '{Data.Count}'");
            }
            else if (Entries.Count == 0)
            {
                if (loadAsyncFromParametersSet == null)
                {
                    Log.Debug("LoadAsync");
                    loadAsyncFromParametersSet = LoadAsync().ContinueWith(t => { loadAsyncFromParametersSet = null; StateHasChanged(); });
                }
                else
                {
                    Log.Debug("LoadAsync skipped due to pending load operation");
                }
            }
        }

        private async Task LoadAsync()
        {
            Ensure.NotNull(DataGetter, "DataGetter");

            try
            {
                IsLoading = true;

                TimelineListResponse response = await DataGetter(offset);

                Entries.AddRange(response.Entries);
                HasMore = response.HasMore;
                offset = Entries.Count;

                Log.Debug($"Loaded '{response.Entries.Count}' ('{(HasMore ? "has more" : "end of stream")}'), total so far '{Entries.Count}'");
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
