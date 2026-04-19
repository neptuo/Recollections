using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Components
{
    public partial class ReleaseNotes : ComponentBase
    {
        [Inject]
        protected ReleaseNotesState State { get; set; }

        [Parameter]
        public string SinceVersion { get; set; }

        private List<ReleaseNotesEntry> entries;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            entries = String.IsNullOrWhiteSpace(SinceVersion)
                ? await State.GetAllAsync()
                : await State.GetSinceAsync(SinceVersion);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);

            if (entries == null)
                return;

            var sb = new StringBuilder();
            foreach (var entry in entries)
                sb.Append(entry.Html);

            builder.AddMarkupContent(0, sb.ToString());
        }
    }
}
