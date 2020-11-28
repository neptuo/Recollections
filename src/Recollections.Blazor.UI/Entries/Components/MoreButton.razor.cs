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
    public partial class MoreButton
    {
        [Inject]
        protected ElementReferenceInterop ElementInterop { get; set; }

        [Parameter]
        public bool IsLoading { get; set; }

        [Parameter]
        public bool HasMore { get; set; }

        [Parameter]
        public EventCallback OnClick { get; set; }

        [Parameter]
        public string Text { get; set; } = "More...";

        [Parameter]
        public string NoMoreText { get; set; } = "Here I started...";

        [Parameter]
        public string LoadingText { get; set; } = "Loading...";

        protected ElementReference Button { get; set; }

        protected async Task LoadAsync()
        {
            if (HasMore && !IsLoading)
            {
                _ = ElementInterop.BlurAsync(Button);
                await OnClick.InvokeAsync();
            }
        }
    }
}
