using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components.Editors
{
    public partial class InlineDateEdit
    {
        protected const string TimeFormat = "HH:mm";
        
        protected DatePicker DatePicker { get; set; }
        protected TimePicker TimePicker { get; set; }

        [Parameter]
        public string Format { get; set; }

        [Parameter]
        public bool IsTimeSelection { get; set; }

        [Parameter]
        public bool ShowAge { get; set; }

        protected Date SelectedDate { get; set; }
        protected Time SelectedTime { get; set; }

        protected override void OnParametersSet() 
        {
            base.OnParametersSet();

            // With no value yet, offer today as the starting point of the picker.
            DateTime pickerValue = Value == DateTime.MinValue ? DateTime.Today : Value;

            SelectedDate = new Date 
            {
                Year = pickerValue.Year, 
                Month = pickerValue.Month,
                Day = pickerValue.Day
            };
            SelectedTime = new Time
            {
                Hour = Value.Hour,
                Minute = Value.Minute,
                Second = Value.Second
            };
        }

        protected void BindValue()
        {
            Value = new DateTime(
                SelectedDate.Year.Value,
                SelectedDate.Month.Value,
                SelectedDate.Day.Value,
                SelectedTime.Hour,
                SelectedTime.Minute,
                SelectedTime.Second
            );
            ValueChanged?.Invoke(Value);
            StateHasChanged();
        }

        protected string GetDateCssClass()
            => IsTimeSelection ? "inline-datetime" : "inline-date";

        protected string GetAgeText()
        {
            int age = BirthDateUtils.GetAge(Value, DateTime.Today);
            return age == 1 ? "1 year" : $"{age} years";
        }
    }
}
