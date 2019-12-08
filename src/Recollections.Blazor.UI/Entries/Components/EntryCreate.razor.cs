using Microsoft.AspNetCore.Components;
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
        protected DatePickerInterop DatePickerInterop { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        public string Title { get; set; }
        public DateTime When { get; set; }

        public List<string> ErrorMessages { get; } = new List<string>();

        protected ElementReference WhenInput { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            When = DateTime.Today;
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await DatePickerInterop.InitializeAsync(WhenInput, UiOptions.DateFormat);
        }

        public async Task CreateAsync()
        {
            await BindWhenFromUi();

            if (Validate())
            {
                EntryModel model = await Api.CreateEntryAsync(new EntryModel(Title, When));
                Navigator.OpenEntryDetail(model.Id);
            }
        }

        private async Task BindWhenFromUi()
        {
            string rawWhen = await DatePickerInterop.GetValueAsync(WhenInput);
            if (DateTime.TryParseExact(rawWhen, UiOptions.DateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out var when))
                When = when;
            else
                When = DateTime.MinValue;
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
