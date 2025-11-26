using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components;

public class TemplatePlaceholder : ComponentBase, IDisposable
{
    private HashSet<TemplateContent> content = new();

    [Inject]
    protected TemplateService Service { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public RenderFragment EmptyContent { get; set; }

    [Parameter]
    public RenderFragment<IReadOnlyCollection<ITemplateContent>> ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Service.DeclarePlaceholder(Name, this);
    }

    public void Dispose()
    {
        Service.DisposePlaceholder(Name, this);
    }

    internal void AddContent(TemplateContent content)
    {
        this.content.Add(content);
        StateHasChanged();
    }

    internal void RemoveContent(TemplateContent content)
    {
        this.content.Remove(content);
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (content.Count == 0)
        {
            builder.AddContent(0, EmptyContent);
            return;
        }

        if (ChildContent != null)
        {
            builder.AddContent(0, ChildContent(content));
            return;
        }

        int i = 2;
        foreach (TemplateContent fragment in content)
        {
            builder.AddContent(i, fragment.ChildContent);
            i++;
        }
    }
}
