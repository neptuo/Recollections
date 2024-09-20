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
    private List<RenderFragment> content = new();

    [Inject]
    protected TemplateService Service { get; set; }

    [Parameter]
    public string Name { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Service.DeclarePlaceholder(Name, this);
    }

    public void Dispose()
    {
        Service.DisposePlaceholder(Name, this);
    }

    internal void AddContent(RenderFragment content)
    {
        this.content.Add(content);
        StateHasChanged();
    }

    internal void RemoveContent(RenderFragment content)
    {
        this.content.Remove(content);
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        for (int i = 0; i < content.Count; i++)
            builder.AddContent(i, content[i]);
    }
}
