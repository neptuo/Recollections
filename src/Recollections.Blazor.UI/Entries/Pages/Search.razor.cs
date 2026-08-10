using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Components;
using Neptuo.Recollections.Entries.Stories;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private string lastSearchUrl;

        /// <summary>
        /// Don't use here. Only for binding purposes.
        /// </summary>
        protected string SearchText { get; set; }
        protected string SearchType { get; set; } = EntrySearchType;
        protected ElementReference SearchInput { get; set; }
        protected BeingPicker BeingPicker { get; set; }
        protected DatePicker DateFromPicker { get; set; }
        protected DatePicker DateToPicker { get; set; }

        protected List<EntryListModel> EntryItems { get; } = [];
        protected List<StoryListModel> StoryItems { get; } = [];
        protected List<string> BeingIds { get; } = [];
        protected DateTime? DateFrom { get; set; }
        protected DateTime? DateTo { get; set; }
        protected bool HasMore { get; private set; }
        protected bool IsLoading { get; set; }
        protected bool HasSearchCriteria => !String.IsNullOrWhiteSpace(Query) || BeingIds.Count > 0 || DateFrom != null || DateTo != null;
        protected bool IsEntryType => SearchType == EntrySearchType;
        protected bool IsStoryType => SearchType == StorySearchType;
        protected Date? DateFromValue => DateFrom == null ? null : new Date(DateFrom.Value);
        protected Date? DateToValue => DateTo == null ? null : new Date(DateTo.Value);

        protected string EmptyMessage => HasSearchCriteria
            ? "Nothing matches the selected filters..."
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

            string currentUrl = Navigator.GetCurrentUrl();
            if (!append && currentUrl == lastSearchUrl)
            {
                Log.Debug($"Not appending and search URL has not changed.");
                return;
            }

            if (!append)
                lastSearchUrl = currentUrl;

            SearchText = Query = Navigator.FindQueryParameter("q");
            SearchType = NormalizeSearchType(Navigator.FindQueryParameter("type"));
            BeingIds.Clear();
            if (IsEntryType)
                BeingIds.AddRange(Navigator.GetQueryString().TryGetValue("being", out var beingIds) ? beingIds.Where(id => !String.IsNullOrWhiteSpace(id)).Distinct() : []);
            DateFrom = ParseDate(Navigator.FindQueryParameter("from"));
            DateTo = ParseDate(Navigator.FindQueryParameter("to"));

            if (!append)
            {
                Log.Debug($"Clearing '{EntryItems.Count}' entry items and '{StoryItems.Count}' story items.");
                EntryItems.Clear();
                StoryItems.Clear();
                offset = 0;
                HasMore = false;
            }

            if (!HasSearchCriteria || (IsStoryType && String.IsNullOrWhiteSpace(Query)))
                return;

            try
            {
                IsLoading = true;

                if (IsEntryType)
                {
                    var response = await Api.SearchEntriesAsync(Query, BeingIds, DateFrom, DateTo, offset);
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
            => Navigator.OpenSearch(SearchText, EntrySearchType, BeingIds, DateFrom, DateTo);

        protected void OpenStorySearch()
            => Navigator.OpenSearch(SearchText, StorySearchType);

        protected void OpenSearch()
        {
            if (IsEntryType)
                OpenEntrySearch();
            else
                OpenStorySearch();
        }

        protected void SelectBeings()
            => BeingPicker.Show(BeingIds);

        protected void OnBeingsSelected(List<string> beingIds)
        {
            BeingIds.Clear();
            BeingIds.AddRange(beingIds);
        }

        protected void SelectDateFrom()
            => DateFromPicker.Show();

        protected void SelectDateTo()
            => DateToPicker.Show();

        protected void OnDateFromSelected(Date date)
        {
            DateFrom = date.ToDateTime();
            StateHasChanged();
        }

        protected void OnDateToSelected(Date date)
        {
            DateTo = date.ToDateTime();
            StateHasChanged();
        }

        protected void ClearDateFrom()
            => DateFrom = null;

        protected void ClearDateTo()
            => DateTo = null;

        private static DateTime? ParseDate(string value)
            => DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
                ? date
                : null;

        protected Task LoadMoreAsync()
            => SearchAsync(true);

        private static string NormalizeSearchType(string value)
            => value == StorySearchType
                ? StorySearchType
                : EntrySearchType;
    }
}
