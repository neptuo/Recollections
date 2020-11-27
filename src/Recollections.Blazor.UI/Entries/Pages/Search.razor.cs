using Microsoft.AspNetCore.Components;
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
        protected Api Api { get; set; }

        [Parameter]
        public string Query { get; set; }

        protected List<object> Items { get; set; }
        protected bool IsLoading { get; set; }

        protected string EmptyMessage
            => String.IsNullOrEmpty(Query)
                ? "Start by filling the search phrase..."
                : $"Nothing matches '{Query}'...";

        protected override void OnInitialized()
        {
            base.OnInitialized();
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

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            await SearchAsync();
        }

        protected async Task SearchAsync()
        {
            Query = Navigator.FindQueryParameter("q");
            if (String.IsNullOrEmpty(Query))
                return;

            try
            {
                IsLoading = true;
                //await Task.Delay(1000);
                await Api.SearchAsync(Query);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
