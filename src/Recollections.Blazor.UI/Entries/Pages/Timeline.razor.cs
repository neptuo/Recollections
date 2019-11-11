using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public class TimelineModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }
        
        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [Inject]
        protected MarkdownConverter MarkdownConverter { get; set; }

        [Inject]
        protected ILog<TimelineModel> Log { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }
        
        private int offset;

        public List<TimelineEntryModel> Entries { get; } = new List<TimelineEntryModel>();
        public bool HasMore { get; private set; }

        protected bool IsEditTextVisible { get; set; }

        protected async override Task OnInitializedAsync()
        {
            Log.Debug("Timeline.Init");

            await base.OnInitializedAsync();
            await UserState.EnsureAuthenticatedAsync();

            Log.Debug("Timeline.Load");
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            TimelineListResponse response = await Api.GetTimelineListAsync(offset);
            Entries.AddRange(response.Entries);
            HasMore = response.HasMore;
            offset = Entries.Count;
        }

        public async Task LoadMoreAsync()
        {
            if (HasMore)
                await LoadAsync();
        }

        protected MarkupString ConvertMarkdown(string text)
        {
            if (text == null)
                return new MarkupString();

            if (text.Length > UiOptions.TextPreviewLength)
                text = text.Substring(0, UiOptions.TextPreviewLength - 3) + "...";

            return new MarkupString(MarkdownConverter.Convert(text));
        }
    }
}
