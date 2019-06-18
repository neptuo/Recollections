using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class EntryCreateModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        public string Title { get; set; }
        public DateTime When { get; set; }

        public List<string> ErrorMessages { get; } = new List<string>();

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            When = DateTime.Today;
        }

        public async Task CreateAsync()
        {
            if (Validate())
            {
                EntryModel model = await Api.CreateAsync(new EntryModel(Title, When));
                Navigator.OpenEntryDetail(model.Id);
            }
        }

        public bool Validate()
        {
            ErrorMessages.Clear();

            if (String.IsNullOrEmpty(Title))
                ErrorMessages.Add("Missing title.");

            if (When == DateTime.MinValue)
                ErrorMessages.Add("When should be something meaningful.");

            return ErrorMessages.Count == 0;
        }
    }
}
