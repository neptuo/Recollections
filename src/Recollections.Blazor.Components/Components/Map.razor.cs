using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class MapModel : ComponentBase
    {
        [Inject]
        protected MapInterop Interop { get; set; }

        [Parameter]
        internal protected int Zoom { get; set; } = 10;

        [Parameter]
        protected ICollection<LocationModel> Markers { get; set; }

        internal ElementRef Container { get; set; }

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();
            await Interop.InitializeAsync(this);

            Console.WriteLine("Map.OnAfterRenderAsync");

            if (Markers != null)
                await Interop.SetMarkers(Markers);
        }
    }
}
