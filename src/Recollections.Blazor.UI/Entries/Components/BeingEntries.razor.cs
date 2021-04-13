using Microsoft.AspNetCore.Components;
using Neptuo.Events;
using Neptuo.Events.Handlers;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Entries.Events;
using Neptuo.Recollections.Entries.Stories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class BeingEntries
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected IEventHandlerCollection EventHandlers { get; set; }

        [Parameter]
        public string BeingId { get; set; }

        [Parameter]
        public string ChapterId { get; set; }

        protected List<BeingEntryModel> Models { get; } = new List<BeingEntryModel>();

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            Models.Clear();

            if (BeingId != null)
                Models.AddRange(await Api.GetBeingEntryListAsync(BeingId));
        }
    }
}
