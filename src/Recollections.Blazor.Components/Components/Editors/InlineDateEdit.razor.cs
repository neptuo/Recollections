using Microsoft.AspNetCore.Components;
using Neptuo.Identifiers;
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

        [Inject]
        protected DatePickerInterop Interop { get; set; }

        protected ElementReference? DateInput { get; set; }

        [Parameter]
        public string Format { get; set; }

        [Parameter]
        public bool IsTimeSelection { get; set; }

        protected string TimeValue { get; set; }

        protected override void OnParametersSet() 
        {
            base.OnParametersSet();

            if (IsTimeSelection)
                TimeValue = Value.ToString(TimeFormat);
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (IsEditMode)
                await Interop.InitializeAsync(DateInput.Value, Format);
        }

        protected async override Task OnValueChangedAsync()
        {
            if (DateInput != null)
            {
                string rawValue = await Interop.GetValueAsync(DateInput.Value);
                if (DateTime.TryParseExact(rawValue, Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out var value))
                    Value = value;

                if (IsTimeSelection && TimeSpan.TryParse(TimeValue, out var time))
                    Value = Value.Date.Add(time);
            }

            await base.OnValueChangedAsync();
        }

        protected string GetDateCssClass()
            => IsTimeSelection ? "inline-datetime" : "inline-date";
    }
}
