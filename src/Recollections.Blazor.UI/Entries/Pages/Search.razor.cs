using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
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

        protected List<EntryListModel> Items { get; } = [];
        protected bool HasMore { get; private set; }
        protected bool IsLoading { get; set; }
        protected bool HasQuery => !String.IsNullOrEmpty(Query);

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

        protected async Task SearchAsync(bool append = false)
        {
            Log.Debug($"Search executed with '{append}'.");

            string lastQuery = Query;
            SearchText = Query = Navigator.FindQueryParameter("q");

            if (!append)
            {
                if (Query == lastQuery)
                {
                    Log.Debug($"Not appending and query not changed (last '{lastQuery}', current '{Query}').");
                    return;
                }

                Log.Debug($"Clearing '{Items.Count}' items.");
                Items.Clear();
                offset = 0;
            }

            if (String.IsNullOrEmpty(Query))
                return;

            try
            {
                IsLoading = true;

                var response = await Api.SearchAsync(Query, offset);
                Items.AddRange(response.Models);
                HasMore = response.HasMore;
                offset = Items.Count;

                Log.Debug($"Found '{response.Models.Count}' items with '{response.HasMore}'.");
            }
            finally
            {
                Log.Debug("Search finished.");
                IsLoading = false;
            }
        }

        protected Task LoadMoreAsync()
            => SearchAsync(true);
    }
}
