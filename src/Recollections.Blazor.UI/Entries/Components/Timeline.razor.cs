using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.JSInterop;
using Neptuo.Logging;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class Timeline(Navigator Navigator, NavigationManager NavigationManager, IJSRuntime JSRuntime, UiOptions UiOptions, ILog<Timeline> Log)
    {
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
        public bool Collapsible { get; set; } = false;

        [Parameter]
        public List<EntryListModel> Data { get; set; }

        [Parameter]
        public Func<int, int?, Task<PageableList<EntryListModel>>> DataGetter { get; set; }

        [Parameter]
        public EventCallback<EntryListModel> OnClick { get; set; }

        private int offset;
        private Task loadAsyncFromParametersSet;
        private string scrollToEntryId;

        protected List<EntryListModel> Entries { get; } = [];
        protected bool HasMore { get; private set; }
        protected bool IsLoading { get; private set; } = true;
        protected bool IsCollapsed { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            Log.Debug("Init");

            await base.OnInitializedAsync();
        }

        private TimelinePosition FindPositionFromHistoryEntry()
        {
            Log.Debug($"Finding timeline position from history entry, state='{NavigationManager.HistoryEntryState}'");
            return PageHistoryState.Parse(NavigationManager.HistoryEntryState).Timeline;
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

                var position = FindPositionFromHistoryEntry();
                if (position != null)
                    scrollToEntryId = position.EntryId;
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (loadAsyncFromParametersSet == null && scrollToEntryId != null)
            {
                var entryId = scrollToEntryId;
                scrollToEntryId = null;
                Log.Debug($"Scrolling to entry '{entryId}'");
                await JSRuntime.InvokeVoidAsync("Timeline.ScrollToEntry", entryId);
            }
        }

        private async Task LoadAsync()
        {
            Ensure.NotNull(DataGetter, "DataGetter");

            try
            {
                IsLoading = true;

                int? count = null;

                Log.Debug($"Loading timeline with offset '{offset}' and count '{count}', current count '{Entries.Count}'");
                if (Entries.Count == 0)
                {
                    var position = FindPositionFromHistoryEntry();
                    if (position != null && position.Offset > 0)
                    {
                        count = position.Offset;
                        scrollToEntryId = position.EntryId;
                        Log.Debug($"Restoring timeline position: offset={position.Offset}, entryId={position.EntryId}");
                    }
                }

                PageableList<EntryListModel> response = await DataGetter(offset, count);

                Entries.AddRange(response.Models);
                HasMore = response.HasMore;
                offset = Entries.Count;

                Log.Debug($"Loaded '{response.Models.Count}' ('{(HasMore ? "has more" : "end of stream")}'), total so far '{Entries.Count}'");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public Task LoadMoreAsync()
            => LoadAsync();

        private async Task OnEntryClicked(EntryListModel entry)
        {
            if (OnClick.HasDelegate)
            {
                await OnClick.InvokeAsync(entry);
                return;
            }

            var position = new TimelinePosition(offset, entry.Id);
            var state = PageHistoryState.Parse(NavigationManager.HistoryEntryState);
            if (state.Timeline != position)
            {
                state.Timeline = position;
                var historyState = state.ToJson();
                Log.Debug($"Saving timeline position to history state: {historyState}");

                NavigationManager.NavigateTo(
                    NavigationManager.Uri,
                    new NavigationOptions
                    {
                        ReplaceHistoryEntry = true,
                        HistoryEntryState = historyState
                    }
                );
            }

            await Task.Yield();
            Navigator.OpenEntryDetail(entry.Id);
        }
    }
}
