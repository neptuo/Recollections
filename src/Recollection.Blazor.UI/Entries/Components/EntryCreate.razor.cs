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
        public string Title { get; set; }
        public DateTime When { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            When = DateTime.Today;
        }

        public async Task CreateAsync()
        {
            Title = null;
            When = DateTime.Today;
        }
    }
}
