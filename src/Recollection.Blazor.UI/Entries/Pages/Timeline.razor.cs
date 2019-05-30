using Microsoft.AspNetCore.Components;
using Neptuo.Recollection.Accounts.Components;
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
        
        [Inject]
        protected Navigator Navigator { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        public List<TimelineEntryModel> Entries { get; } = new List<TimelineEntryModel>();

        protected async override Task OnInitAsync()
        {
            Console.WriteLine("Timeline.Init");

            await base.OnInitAsync();
            await UserState.EnsureAuthenticated();

            Console.WriteLine("Timeline.Load");
            TimelineListResponse response = await Api.GetListAsync(new TimelineListRequest());
            Entries.AddRange(response.Entries);
        }

        public async Task DeleteAsync(string entryId, string title)
        {
            if (await Navigator.AskAsync($"Do you really want to delete entry '{title}'?"))
            {
                await Api.DeleteAsync(entryId);
                Entries.Remove(Entries.Single(e => e.Id == entryId));
            }
        }
    }
}
