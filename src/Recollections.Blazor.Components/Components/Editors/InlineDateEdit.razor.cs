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
        protected InlineDateEditInterop Interop { get; set; }

        [Inject]
        protected IUniqueNameProvider NameProvider { get; set; }

        public string InputId { get; private set; }

        [Parameter]
        protected string Format { get; set; }

        protected override void OnInit()
        {
            base.OnInit();
            InputId = NameProvider.Next();
        }

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();

            if (IsEditMode)
                await Interop.InitializeAsync(InputId, Format);
            else
                await Interop.DestroyAsync(InputId);
        }

        protected async override Task OnValueChangedAsync()
        {
            string rawValue = await Interop.GetValueAsync(InputId);
            if (DateTime.TryParseExact(rawValue, Format, CultureInfo.CurrentCulture, DateTimeStyles.None, out var value))
                Value = value;

            await base.OnValueChangedAsync();
        }
    }
}
