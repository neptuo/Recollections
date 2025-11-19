using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Entries.Beings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class Beings
    {
        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        protected bool IsLoading { get; set; }

        public string Name { get; set; }
        public List<string> ErrorMessages { get; } = new List<string>();

        public List<BeingListModel> Items { get; } = new List<BeingListModel>();

        protected async override Task OnInitializedAsync()
        {
            IsLoading = true;

            await base.OnInitializedAsync();
            await LoadDataAsync();
        }

        protected async Task LoadDataAsync()
        {
            IsLoading = true;
            Items.Clear();
            Items.AddRange(await Api.GetBeingListAsync());
            IsLoading = false;
        }

        protected async Task CreateAsync()
        {
            var model = new BeingModel()
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name
            };

            await Api.CreateBeingAsync(model);
            Navigator.OpenBeingDetail(model.Id);
        }
    }
}
