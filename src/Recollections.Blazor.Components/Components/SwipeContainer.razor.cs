using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Neptuo.Recollections.Components;

public partial class SwipeContainer : ComponentBase, IAsyncDisposable
{
    [Inject]
    protected IJSRuntime JsRuntime { get; set; }

    [Parameter]
    public RenderFragment PreviousContent { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public RenderFragment NextContent { get; set; }

    [Parameter]
    public EventCallback<string> OnSwiped { get; set; }

    [Parameter]
    public string CssClass { get; set; }

    private ElementReference container;
    private IJSObjectReference module;
    private DotNetObjectReference<SwipeContainer> selfRef;
    private bool initialized;
    private bool pendingReset;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!initialized)
        {
            module ??= await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Recollections.Blazor.Components/SwipeContainer.js");
            selfRef ??= DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("initialize", selfRef, container);
            initialized = true;
        }
        else if (pendingReset)
        {
            pendingReset = false;
            await module.InvokeVoidAsync("resetPosition", container);
        }
    }

    [JSInvokable]
    public async Task OnSwipeCompleted(string direction)
    {
        pendingReset = true;
        await OnSwiped.InvokeAsync(direction);
    }

    public async ValueTask DisposeAsync()
    {
        if (module != null)
        {
            try
            {
                await module.InvokeVoidAsync("dispose", container);
            }
            catch (JSDisconnectedException)
            {
            }

            await module.DisposeAsync();
        }

        selfRef?.Dispose();
    }
}
