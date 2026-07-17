using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
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
    public partial class Search : IDisposable
    {
        private const string EntrySearchType = "entry";
        private const string StorySearchType = "story";

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected ILog<Search> Log { get; set; }

        [Inject]
        protected Api Api { get; set; }

        [Parameter]
        public string Query { get; set; }

        private int offset;

        /// <summary>
        /// Don't use here. Only for binding purposes.
        /// </summary>
        protected string SearchText { get; set; }
        protected string SearchType { get; set; } = EntrySearchType;
        protected ElementReference SearchInput { get; set; }

        protected List<EntryListModel> EntryItems { get; } = [];
        protected List<StoryListModel> StoryItems { get; } = [];
        protected bool HasMore { get; private set; }
        protected bool IsLoading { get; set; }
        protected bool HasQuery => !String.IsNullOrEmpty(Query);
        protected bool IsEntryType => SearchType == EntrySearchType;
        protected bool IsStoryType => SearchType == StorySearchType;

        protected string EmptyMessage => HasQuery
            ? $"Nothing matches '{Query}'..."
            : "Start by filling the search phrase...";

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Navigator.LocationChanged += OnLocationChanged;
        }

        public void Dispose()
        {
            Navigator.LocationChanged -= OnLocationChanged;
        }

        private async void OnLocationChanged(string url)
        {
            await SearchAsync();
            StateHasChanged();
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            Log.Debug("SetParametersAsync");
            return base.SetParametersAsync(parameters);
        }

        protected override async Task OnParametersSetAsync()
        {
            Log.Debug("OnParametersSetAsync");
            await base.OnParametersSetAsync();
            await SearchAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await SearchInput.FocusAsync();

            await base.OnAfterRenderAsync(firstRender);
        }

        protected async Task SearchAsync(bool append = false)
        {
            Log.Debug($"Search executed with '{append}'.");

            string lastQuery = Query;
            string lastType = SearchType;
            SearchText = Query = Navigator.FindQueryParameter("q");
            SearchType = NormalizeSearchType(Navigator.FindQueryParameter("type"));

            if (!append)
            {
                if (Query == lastQuery && SearchType == lastType)
                {
                    Log.Debug($"Not appending and query/type not changed (last '{lastQuery}'/'{lastType}', current '{Query}'/'{SearchType}').");
                    return;
                }

                Log.Debug($"Clearing '{EntryItems.Count}' entry items and '{StoryItems.Count}' story items.");
                EntryItems.Clear();
                StoryItems.Clear();
                offset = 0;
                HasMore = false;
            }

            if (String.IsNullOrEmpty(Query))
                return;

            try
            {
                IsLoading = true;

                if (IsEntryType)
                {
                    var response = await Api.SearchEntriesAsync(Query, offset);
                    EntryItems.AddRange(response.Models);
                    HasMore = response.HasMore;
                    offset = EntryItems.Count;
                    Log.Debug($"Found '{response.Models.Count}' entry items with '{response.HasMore}'.");
                }
                else
                {
                    var response = await Api.SearchStoriesAsync(Query, offset);
                    StoryItems.AddRange(response.Models);
                    HasMore = response.HasMore;
                    offset = StoryItems.Count;
                    Log.Debug($"Found '{response.Models.Count}' story items with '{response.HasMore}'.");
                }
            }
            finally
            {
                Log.Debug("Search finished.");
                IsLoading = false;
            }
        }

        protected void OpenEntrySearch()
            => Navigator.OpenSearch(SearchText, EntrySearchType);

        protected void OpenStorySearch()
            => Navigator.OpenSearch(SearchText, StorySearchType);

        protected Task LoadMoreAsync()
            => SearchAsync(true);

        private static string NormalizeSearchType(string value)
            => value == StorySearchType
                ? StorySearchType
                : EntrySearchType;
    }
}
