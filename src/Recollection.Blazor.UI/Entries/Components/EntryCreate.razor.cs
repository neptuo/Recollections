using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries.Components
{
    public class EntryCreateModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        public string Title { get; set; }
        public DateTime When { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            When = DateTime.Today;
        }

        public async Task CreateAsync()
        {
            await Api.CreateAsync(new EntryModel(Title, When));

            Title = null;
            When = DateTime.Today;
        }
    }
}
