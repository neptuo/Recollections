using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Entries.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public partial class Calendar
    {
        [Inject]
        protected ILog<Calendar> Log { get; set; }

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

        protected DatePicker DatePicker { get; set; }

        protected Date SelectedDate { get; set; }
        protected bool IsMonthView => Month != null;
        protected string MonthTitle => $"{DateTimeFormatInfo.CurrentInfo.MonthNames[Month.Value - 1]} {Year}";

        protected List<CalendarEntryModel> Models { get; } = new List<CalendarEntryModel>();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await UserState.EnsureAuthenticatedAsync();
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            int? prevYear = Year;
            int? prevMonth = Month;

            await base.SetParametersAsync(parameters);

            Log.Info($"Parameters: Year='{Year}', Month='{Month}', prev('{prevYear}', '{prevMonth}').");

            if (Year == null)
            {
                Year = DateTime.Now.Year;
                Month = DateTime.Now.Month;
            }

            SelectedDate = new Date()
            {
                Year = Year,
                Month = Month
            };

            if (Year != prevYear || Month != prevMonth)
                await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (IsMonthView)
            {
                Models.Clear();
                Models.AddRange(await Api.GetMonthEntryListAsync(Year.Value, Month.Value));
            }

            StateHasChanged();
        }

        protected string GetPrevPeriodUrl()
        {
            int? year = Year;
            int? month = Month;

            if (IsMonthView)
                (year, month) = DatePicker.PrevMonth(year.Value, month.Value);

            return Navigator.UrlCalendar(year, month);
        }

        protected string GetNextPeriodUrl()
        {
            int? year = Year;
            int? month = Month;

            if (IsMonthView)
                (year, month) = DatePicker.NextMonth(year.Value, month.Value);

            return Navigator.UrlCalendar(year, month);
        }

        protected void OnDatePicked(Date date)
        {
            if (date.Year != null && date.Month != null)
                Navigator.OpenCalendar(date.Year, date.Month);
        }
    }
}
