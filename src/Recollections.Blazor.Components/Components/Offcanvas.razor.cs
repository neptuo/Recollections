using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components;

public partial class Offcanvas : System.IDisposable
{
    protected ElementReference Element { get; set; }

    [Inject]
    protected ILog<Offcanvas> Log { get; set; }

    [Inject]
    protected OffcanvasInterop Interop { get; set; }

    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public RenderFragment HeaderContent { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> Attributes { get; set; }

    public bool IsVisible { get; private set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        var cssClass = "offcanvas offcanvas-bottom";
        if (Attributes.TryGetValue("class", out var providedCssClass))
            cssClass += " " + providedCssClass;

        Attributes["class"] = cssClass;
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        
        if (firstRender)
            await Interop.InitializeAsync(this, Element);
    }

    internal void UpdateVisibility(bool isVisible) 
    {
        IsVisible = isVisible;
        StateHasChanged();
    }

    public void Show() => Interop.Show(Element);
    public void Hide() => Interop.Hide(Element);

    public void Dispose()
    {
        Interop.Dispose(Element);
    }
}