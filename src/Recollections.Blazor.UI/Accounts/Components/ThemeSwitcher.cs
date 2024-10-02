using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Neptuo.Logging;
using Neptuo.Recollections.Components;

namespace Neptuo.Recollections.Accounts.Components;

public partial class ThemeSwitcher : ComponentBase, IDisposable
{
    [Inject]
    protected PropertyCollection Properties { get; set; }

    [Inject]
    protected ThemeInterop Interop { get; set; }

    [Inject]
    protected ILog<ThemeSwitcher> Log { get; set; }

    protected ThemeType Theme { get; set; }

    protected async override Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await ApplyThemeAsync();

        Properties.ValuesLoaded += OnPropertiesLoaded;
    }

    public void Dispose()
    {
        Properties.ValuesLoaded -= OnPropertiesLoaded;
    }

    private void OnPropertiesLoaded()
        => _ = ApplyThemeAsync();

    private async Task ApplyThemeAsync()
    {
        var previousTheme = Theme;
        Log.Debug($"ApplyThemeAsync '{Theme}', '{previousTheme}'");

        Theme = await Properties.ThemeAsync();
        Log.Debug($"Got theme '{Theme}', previous was '{previousTheme}'");

        if (previousTheme == Theme)
            return;

        Interop.Apply(Theme switch {
            ThemeType.Light => "light",
            ThemeType.Dark => "dark",
            ThemeType.Auto => Interop.GetBrowserPreference(),
            _ => throw Ensure.Exception.NotSupported(Theme)
        });
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    { }
}