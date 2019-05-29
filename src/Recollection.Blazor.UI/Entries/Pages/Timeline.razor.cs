using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries.Pages
{
    public class TimelineModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        public List<TimelineEntryModel> Entries { get; } = new List<TimelineEntryModel>();

        protected async override Task OnInitAsync()
        {
            await base.OnInitAsync();

            Console.WriteLine("Timeline.List");
            TimelineListResponse response = await Api.GetListAsync(new TimelineListRequest());
            Entries.AddRange(response.Entries);
        }
    }
}
