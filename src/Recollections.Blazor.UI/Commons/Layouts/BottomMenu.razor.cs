using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Layouts;

public partial class BottomMenu : IDisposable
{
    [Inject]
    internal Navigator Navigator { get; set; }

    protected Offcanvas Offcanvas { get; set; }

    [Parameter]
    public MenuList Menu { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Navigator.LocationChanged += OnLocationChanged;
    }

    public void Dispose()
    {
        Navigator.LocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged(string location)
    {
        Offcanvas.Hide();
    }

    protected void OnToggleMainMenu()
    {
        if (Offcanvas.IsVisible)
            Offcanvas.Hide();
        else
            Offcanvas.Show();
    }
}
