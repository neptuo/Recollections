using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class EntryPicker
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Parameter]
        public Action<EntryListModel> Selected { get; set; }

        protected Modal Modal { get; set; }

        private int offset;
        private bool isFirstShow = true;

        protected bool IsLoading { get; set; }
        protected List<EntryListModel> Entries { get; } = new List<EntryListModel>();
        protected bool HasMore { get; private set; }

        private async Task LoadAsync()
        {
            IsLoading = true;
            var response = await Api.GetTimelineListAsync(offset);
            Entries.AddRange(response.Models);
            HasMore = response.HasMore;
            offset = Entries.Count;
            IsLoading = false;

            StateHasChanged();
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
