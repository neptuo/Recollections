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
    public partial class Timeline(Navigator Navigator, UiOptions UiOptions, ILog<Timeline> Log)
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
        public Func<int, Task<PageableList<EntryListModel>>> DataGetter { get; set; }

        [Parameter]
        public EventCallback<EntryListModel> OnClick { get; set; }

        private int offset;
        private Task loadAsyncFromParametersSet;

        protected List<EntryListModel> Entries { get; } = [];
        protected bool HasMore { get; private set; }
        protected bool IsLoading { get; private set; } = true;
        protected bool IsCollapsed { get; set; } = false;

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

                PageableList<EntryListModel> response = await DataGetter(offset);

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
    }
}
