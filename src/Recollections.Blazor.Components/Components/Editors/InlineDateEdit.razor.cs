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

        protected ElementReference Input { get; set; }

        [Parameter]
        public string Format { get; set; }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (IsEditMode)
                await Interop.InitializeAsync(Input, Format);
            else
                await Interop.DestroyAsync(Input);
        }

        protected async override Task OnValueChangedAsync()
        {
            string rawValue = await Interop.GetValueAsync(Input);
            if (DateTime.TryParseExact(rawValue, Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out var value))
                Value = value;

            await base.OnValueChangedAsync();
        }
    }
}
