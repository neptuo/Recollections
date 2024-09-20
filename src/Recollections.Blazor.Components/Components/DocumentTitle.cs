using Microsoft.AspNetCore.Components;
using Neptuo.Events;
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

        [Inject]
        protected IEventDispatcher Events { get; set; }

        [Parameter]
        public string Value { get; set; }

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            string formatted = String.IsNullOrEmpty(Value) ? Suffix : $"{Value} - {Suffix}";
            await Interop.SetAsync(formatted);
            await Events.PublishAsync(new DocumentTitleChanged(Value, formatted));
        }

        public void Dispose()
        {
            _ = Interop.SetAsync(Suffix);
        }
    }

    public record DocumentTitleChanged(string Value, string Formatted);
}
