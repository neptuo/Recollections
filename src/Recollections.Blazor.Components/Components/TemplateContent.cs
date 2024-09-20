using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components;

public class TemplateContent : ComponentBase, IDisposable
{
    [Inject]
    protected TemplateService Service { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Service.AddContent(Name, ChildContent);
    }

    public void Dispose()
    {
        Service.RemoveContent(Name, ChildContent);
    }
}
