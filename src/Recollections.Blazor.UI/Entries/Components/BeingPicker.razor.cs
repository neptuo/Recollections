using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Beings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class BeingPicker
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected ILog<BeingPicker> Log { get; set; }

        [Parameter]
        public EventCallback<List<string>> Selected { get; set; }

        protected Modal Modal { get; set; }

        private bool isFirstShow = true;

        protected bool IsLoading { get; set; }
        protected List<BeingListModel> Beings { get; } = new List<BeingListModel>();
        protected List<string> SelectedIds { get; } = new List<string>();

        private async Task LoadAsync()
        {
            IsLoading = true;
            Beings.Clear();
            Beings.AddRange(await Api.GetBeingListAsync());
            IsLoading = false;

            StateHasChanged();
        }

        private void OnItemClick(string id)
        {
            if (SelectedIds.Contains(id))
                SelectedIds.Remove(id);
            else
                SelectedIds.Add(id);

            Log.Info($"Selected beings: '{String.Join(", ", SelectedIds)}'.");
        }

        private async void OnSubmit()
        {
            Hide();
            _ = Selected.InvokeAsync(SelectedIds);
        }

        public void Show(IEnumerable<string> beingIds)
        {
            Ensure.NotNull(beingIds, "beingIds");

            SelectedIds.Clear();
            SelectedIds.AddRange(beingIds);
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
