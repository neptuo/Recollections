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
    public class InlineDateEditModel : InlineEditModel<DateTime>
    {
        [Inject]
        protected DatePickerInterop Interop { get; set; }

        protected ElementReference? Input { get; set; }

        [Parameter]
        public string Format { get; set; }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (IsEditMode)
                await Interop.InitializeAsync(Input.Value, Format);
        }

        protected async override Task OnValueChangedAsync()
        {
            if (Input != null)
            {
                string rawValue = await Interop.GetValueAsync(Input.Value);
                if (DateTime.TryParseExact(rawValue, Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out var value))
                    Value = value;
            }

            await base.OnValueChangedAsync();
        }
    }
}
