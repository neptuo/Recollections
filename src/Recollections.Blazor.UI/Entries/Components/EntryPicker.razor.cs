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
    public partial class EntryPicker(Api Api)
    {
        [Parameter]
        public Action<EntryListModel> Selected { get; set; }

        protected Modal Modal { get; set; }

        private bool wasVisible = false;

        public void Show()
        {
            wasVisible = true;
            StateHasChanged();

            Modal.Show();
        }

        public void Hide() => Modal.Hide();
    }
}
