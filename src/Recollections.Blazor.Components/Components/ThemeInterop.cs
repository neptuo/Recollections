using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components;

public class ThemeInterop
{
    private readonly IJSInProcessRuntime jsRuntime;

    public ThemeInterop(IJSRuntime jsRuntime)
    {
        Ensure.NotNull(jsRuntime, "jsRuntime");
        this.jsRuntime = (IJSInProcessRuntime)jsRuntime;
    }

    public void Apply(string theme) => jsRuntime.InvokeVoid("Bootstrap.Theme.Apply", theme);
    public string GetBrowserPreference() => jsRuntime.Invoke<string>("Bootstrap.Theme.GetBrowserPreference");
}
