using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Neptuo.Events;
using Neptuo.Events.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components;

public class DocumentTitleViewer : ComponentBase, IDisposable, IEventHandler<DocumentTitleChanged>
{
    [Inject]
    internal IEventHandlerCollection EventHandlers { get; set; }

    protected string Value { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        EventHandlers.Add<DocumentTitleChanged>(this);
    }

    public void Dispose()
    {
        EventHandlers.Remove<DocumentTitleChanged>(this);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, Value);
    }

    Task IEventHandler<DocumentTitleChanged>.HandleAsync(DocumentTitleChanged payload)
    {
        Value = payload.Value;
        StateHasChanged();
        return Task.CompletedTask;
    }
}
