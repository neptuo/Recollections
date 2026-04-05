using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class EntryPicker(Api Api)
    {
        protected enum Tab
        {
            Timeline,
            Search
        }

        [Parameter]
        public Action<EntryListModel> Selected { get; set; }

        protected Modal Modal { get; set; }

        private bool wasVisible = false;

        protected Tab ActiveTab { get; set; } = Tab.Timeline;
        protected string SearchText { get; set; }
        protected List<EntryListModel> SearchResults { get; } = [];
        protected bool SearchHasMore { get; set; }
        protected bool IsSearching { get; set; }
        private int searchOffset;

        public void Show()
        {
            wasVisible = true;
            ActiveTab = Tab.Timeline;
            SearchText = null;
            SearchResults.Clear();
            SearchHasMore = false;
            searchOffset = 0;
            StateHasChanged();

            Modal.Show();
        }

        public void Hide() => Modal.Hide();

        protected void SwitchTab(Tab tab)
        {
            ActiveTab = tab;
        }

        protected async Task SearchAsync()
        {
            if (string.IsNullOrEmpty(SearchText))
                return;

            SearchResults.Clear();
            searchOffset = 0;
            await LoadSearchResultsAsync();
        }

        protected async Task LoadMoreSearchResultsAsync()
        {
            await LoadSearchResultsAsync();
        }

        private async Task LoadSearchResultsAsync()
        {
            try
            {
                IsSearching = true;

                var response = await Api.SearchAsync(SearchText, searchOffset);
                SearchResults.AddRange(response.Models);
                SearchHasMore = response.HasMore;
                searchOffset = SearchResults.Count;
            }
            finally
            {
                IsSearching = false;
            }
        }

        protected void OnEntrySelected(EntryListModel entry)
        {
            Hide();
            Selected?.Invoke(entry);
        }
    }
}
