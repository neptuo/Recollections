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

        protected Date SelectedDate { get; set; }
        protected Time SelectedTime { get; set; }

        protected override void OnParametersSet() 
        {
            base.OnParametersSet();

            SelectedDate = new Date 
            {
                Year = Value.Year, 
                Month = Value.Month,
                Day = Value.Day
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
    }
}
