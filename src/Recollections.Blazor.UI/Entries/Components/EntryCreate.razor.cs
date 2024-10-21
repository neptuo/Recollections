using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class EntryCreate
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected ElementReferenceInterop ElementInterop { get; set; }

        public string Title { get; set; }
        public Date When { get; set; }

        public List<string> ErrorMessages { get; } = new List<string>();

        protected DatePicker DatePicker { get; set; }
        protected ElementReference WhenInput { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            When = new Date(DateTime.Today);
        }

        public async Task CreateAsync()
        {
            await BindWhenFromUi();

            if (Validate())
            {
                EntryModel model = await Api.CreateEntryAsync(new EntryModel(Title, When.ToDateTime()));
                Navigator.OpenEntryDetail(model.Id);
            }
        }

        private async Task BindWhenFromUi()
        {
            string rawWhen = await ElementInterop.GetValueAsync(WhenInput);
            if (DateTime.TryParseExact(rawWhen, UiOptions.DateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out var when))
                When = new Date(when);
            else
                When = new Date(DateTime.MinValue);
        }

        public bool Validate()
        {
            ErrorMessages.Clear();

            if (String.IsNullOrEmpty(Title))
                ErrorMessages.Add("Missing title.");

            if (When.ToDateTime() == DateTime.MinValue)
                ErrorMessages.Add("When should be something meaningful.");

            return ErrorMessages.Count == 0;
        }
    }
}
