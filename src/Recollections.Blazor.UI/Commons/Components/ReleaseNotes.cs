using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
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
                AppendEntry(sb, entry);

            builder.AddMarkupContent(0, sb.ToString());
        }

        private static void AppendEntry(StringBuilder sb, ReleaseNotesEntry entry)
        {
            AppendSection(sb, "Breaking changes", entry.BreakingChanges);
            AppendSection(sb, "New features", entry.NewFeatures);
            AppendSection(sb, "Bug fixes", entry.BugFixes);

            sb.AppendLine($@"<div class=""row"">
    <div class=""col-12 col-md-auto"">
        <a target=""_blank"" rel=""noopener noreferrer"" href=""https://github.com/neptuo/Recollections/milestone/{entry.Milestone}?closed=1"" class=""btn bg-light-subtle w-100"">
            <span class=""fab fa-github""></span>
            See details on GitHub
        </a>
    </div>
</div>");
        }

        private static void AppendSection(StringBuilder sb, string heading, List<string> items)
        {
            if (items == null || items.Count == 0)
                return;

            sb.AppendLine($"<h3>{HtmlEncoder.Default.Encode(heading)}</h3>");
            sb.AppendLine("<ul>");
            foreach (var item in items)
                sb.AppendLine($"    <li>{HtmlEncoder.Default.Encode(item)}</li>");
            sb.AppendLine("</ul>");
        }
    }
}
