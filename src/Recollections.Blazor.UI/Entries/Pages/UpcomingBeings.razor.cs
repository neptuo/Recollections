using Neptuo.Recollections.Entries.Beings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class UpcomingBeings
    {
        protected bool IncludeBirthdays { get; set; } = true;
        protected bool IncludeNameDays { get; set; } = true;
        protected bool IsLoading { get; set; }
        protected List<UpcomingBeingModel> Items { get; } = [];
        private List<UpcomingBeingModel> allItems = [];

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadAsync();
        }

        protected async Task LoadAsync()
        {
            IsLoading = true;
            allItems = await Api.GetUpcomingBeingListAsync();
            ApplyFilters();
            IsLoading = false;
        }

        private void ApplyFilters()
        {
            Items.Clear();
            Items.AddRange(allItems.Where(item => (item.IsBirthday && IncludeBirthdays) || (!item.IsBirthday && IncludeNameDays)));
        }
    }
}
