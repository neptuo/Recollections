using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class EntryPickerModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Parameter]
        public Action<TimelineEntryModel> Selected { get; set; }

        protected ModalModel Modal { get; set; }

        private int offset;
        private bool isFirstShow = true;

        protected List<TimelineEntryModel> Entries { get; } = new List<TimelineEntryModel>();
        protected bool HasMore { get; private set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            TimelineListResponse response = await Api.GetTimelineListAsync(offset);
            Entries.AddRange(response.Entries);
            HasMore = response.HasMore;
            offset = Entries.Count;
        }

        protected async Task LoadMoreAsync()
        {
            if (HasMore)
                await LoadAsync();
        }

        public void Show()
        {
            Modal.Show();

            if (isFirstShow)
            {
                isFirstShow = false;
                _ = LoadAsync();
            }
        }

        public void Hide() => Modal.Hide();
    }
}
