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
        public Action<Date> ValueChanged { get; set; }

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

            CurrentPart = Part;

            if (Value != null)
            {
                CurrentYear = Value.Value.Year ?? DateTime.Now.Year;
                CurrentMonth = Value.Value.Month ?? DateTime.Now.Month;
            }
            else
            {
                CurrentYear = DateTime.Now.Year;
                CurrentMonth = DateTime.Now.Month;
            }
        }

        protected void OnYearSelected(int year)
        {
            if (Part == DatePickerPart.Year)
            {
                Hide();
                ValueChanged?.Invoke(new Date()
                {
                    Year = year,
                });
            }
            else
            {
                CurrentYear = year;
                CurrentPart = DatePickerPart.Month;
            }
        }

        protected void OnMonthSelected(int month)
        {
            if (Part == DatePickerPart.Month)
            {
                Hide();
                ValueChanged?.Invoke(new Date()
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

        protected void OnDaySelected(int day)
        {
            if (Part == DatePickerPart.Day)
            {
                Hide();
                ValueChanged?.Invoke(new Date()
                {
                    Year = CurrentYear,
                    Month = CurrentMonth,
                    Day = day
                });
            }
        }

        protected void OnTodaySelected()
        {
            var today = DateTime.Today;
            switch (Part)
            {
                case DatePickerPart.Year:
                    OnYearSelected(today.Year);
                    break;
                case DatePickerPart.Month:
                    CurrentYear = today.Year;
                    OnMonthSelected(today.Month);
                    break;
                case DatePickerPart.Day:
                    CurrentYear = today.Year;
                    CurrentMonth = today.Month;
                    OnDaySelected(today.Day);
                    break;
                default:
                    throw Ensure.Exception.NotSupported(Part);
            }
        }

        public void Show() => Modal.Show();
        public void Hide() => Modal.Hide();

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
    }

    public enum DatePickerPart
    {
        Year,
        Month,
        Day
    }
}
