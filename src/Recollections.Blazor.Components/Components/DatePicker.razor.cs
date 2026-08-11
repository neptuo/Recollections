using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class DatePicker
    {
        [Parameter]
        public DatePickerPart Part { get; set; }

        [Parameter]
        public Date? Value { get; set; }

        [Parameter]
        public EventCallback<Date> ValueChanged { get; set; }

        [Parameter]
        public bool AllowClear { get; set; }

        [Parameter]
        public bool AllowYearSelection { get; set; } = true;

        protected Modal Modal { get; set; }

        protected string Title
        {
            get
            {
                string period = null;
                switch (CurrentPart)
                {
                    case DatePickerPart.Year:
                        period = "year";
                        break;
                    case DatePickerPart.Month:
                        period = "month";
                        break;
                    case DatePickerPart.Day:
                        period = "day";
                        break;
                    default:
                        throw Ensure.Exception.NotSupported(CurrentPart);
                }

                return $"Select a {period}";
            }
        }

        protected DatePickerPart CurrentPart { get; set; }
        protected string[] MonthNames => DateTimeFormatInfo.CurrentInfo.MonthNames;

        protected int CurrentYear { get; set; }
        protected int CurrentMonth { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            CurrentPart = AllowYearSelection ? Part : DatePickerPart.Month;

            if (Value != null)
            {
                CurrentYear = AllowYearSelection
                    ? Value.Value.Year ?? DateTime.Now.Year
                    : 2000;
                CurrentMonth = Value.Value.Month ?? DateTime.Now.Month;
            }
            else
            {
                CurrentYear = AllowYearSelection ? DateTime.Now.Year : 2000;
                CurrentMonth = DateTime.Now.Month;
            }
        }

        protected async Task OnYearSelected(int year)
        {
            if (Part == DatePickerPart.Year)
            {
                Hide();
                await ValueChanged.InvokeAsync(new Date()
                {
                    Year = year,
                });
            }
            else
            {
                if (AllowYearSelection)
                {
                    CurrentYear = year;
                    CurrentPart = DatePickerPart.Month;
                }
            }
        }

        protected async Task OnMonthSelected(int month)
        {
            if (Part == DatePickerPart.Month)
            {
                Hide();
                await ValueChanged.InvokeAsync(new Date()
                {
                    Year = CurrentYear,
                    Month = month
                });
            }
            else
            {
                CurrentMonth = month;
                CurrentPart = DatePickerPart.Day;
            }
        }

        protected async Task OnDaySelected(int day)
        {
            if (Part == DatePickerPart.Day)
            {
                Hide();
                await ValueChanged.InvokeAsync(new Date()
                {
                    Year = CurrentYear,
                    Month = CurrentMonth,
                    Day = day
                });
            }
        }

        protected void OnDayHeaderClicked()
        {
            if (AllowYearSelection)
                CurrentPart = DatePickerPart.Month;
        }

        protected async Task OnTodaySelected()
        {
            var today = DateTime.Today;
            switch (Part)
            {
                case DatePickerPart.Year:
                    await OnYearSelected(today.Year);
                    break;
                case DatePickerPart.Month:
                    if (AllowYearSelection)
                        CurrentYear = today.Year;

                    await OnMonthSelected(today.Month);
                    break;
                case DatePickerPart.Day:
                    if (AllowYearSelection)
                        CurrentYear = today.Year;

                    CurrentMonth = today.Month;
                    await OnDaySelected(today.Day);
                    break;
                default:
                    throw Ensure.Exception.NotSupported(Part);
            }
        }

        public void Show() => Modal.Show();
        public void Hide() => Modal.Hide();

        protected async Task OnClearSelected()
        {
            Hide();
            await ValueChanged.InvokeAsync(new Date());
        }

        public static (int year, int month) PrevMonth(int year, int month)
        {
            if (month > 1)
            {
                month--;
            }
            else
            {
                year--;
                month = 12;
            }

            return (year, month);
        }

        public static (int year, int month) NextMonth(int year, int month)
        {
            if (month < 12)
            {
                month++;
            }
            else
            {
                year++;
                month = 1;
            }

            return (year, month);
        }
    }

    public struct Date
    {
        public int? Year;
        public int? Month;
        public int? Day;

        public Date()
        { }

        public Date(DateTime dateTime)
        {
            Year = dateTime.Year;
            Month = dateTime.Month;
            Day = dateTime.Day;
        }

        public DateTime ToDateTime() => Year == null || Month == null || Day == null 
            ? DateTime.MinValue 
            : new DateTime(Year.Value, Month.Value, Day.Value);
    }

    public enum DatePickerPart
    {
        Year,
        Month,
        Day
    }
}
