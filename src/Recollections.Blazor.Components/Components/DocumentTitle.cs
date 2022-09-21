using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class DocumentTitle : ComponentBase, IDisposable
    {
        private const string Suffix = "Recollections";

        [Inject]
        protected DocumentTitleInterop Interop { get; set; }

        [Parameter]
        public string Value { get; set; }

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (String.IsNullOrEmpty(Value))
                await Interop.SetAsync(Suffix);
            else
                await Interop.SetAsync($"{Value} - {Suffix}");
        }

        public void Dispose()
        {
            _ = Interop.SetAsync(Suffix);
        }
    }
}
