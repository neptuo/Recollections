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
        
        private int offset;

        public List<TimelineEntryModel> Entries { get; } = new List<TimelineEntryModel>();
        public bool HasMore { get; private set; }

        protected bool IsEditTextVisible { get; set; }

        protected async override Task OnInitAsync()
        {
            Console.WriteLine("Timeline.Init");

            await base.OnInitAsync();
            await UserState.EnsureAuthenticated();

            Console.WriteLine("Timeline.Load");
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            TimelineListResponse response = await Api.GetListAsync(offset);
            Entries.AddRange(response.Entries);
            HasMore = response.HasMore;
            offset = Entries.Count;
        }

        public async Task DeleteAsync(string entryId, string title)
        {
            if (await Navigator.AskAsync($"Do you really want to delete entry '{title}'?"))
            {
                await Api.DeleteAsync(entryId);
                Entries.Remove(Entries.Single(e => e.Id == entryId));
            }
        }

        public async Task LoadMoreAsync()
        {
            if (HasMore)
                await LoadAsync();
        }
    }
}
