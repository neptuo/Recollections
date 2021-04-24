using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class Calendar
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        [Parameter]
        public int? Year { get; set; }

        [Parameter]
        public int? Month { get; set; }

        protected bool IsMonthView => Month != null;

        protected List<CalendarEntryModel> Models { get; } = new List<CalendarEntryModel>();

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            await UserState.EnsureAuthenticatedAsync();

            if (Year == null)
            {
                Year = DateTime.Now.Year;
                Month = DateTime.Now.Month;
            }

            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (IsMonthView)
            {
                Models.Clear();
                Models.AddRange(await Api.GetMonthEntryListAsync(Year.Value, Month.Value));
            }
        }

        protected async Task PrevPeriodAsync()
        {
            if (IsMonthView)
            {
                if (Month > 1)
                {
                    Month--;
                }
                else
                {
                    Year--;
                    Month = 12;
                }
            }

            await LoadDataAsync();
        }

        protected async Task NextPeriodAsync()
        {
            if (IsMonthView)
            {
                if (Month < 12)
                {
                    Month++;
                }
                else
                {
                    Year++;
                    Month = 1;
                }
            }

            await LoadDataAsync();
        }
    }
}
