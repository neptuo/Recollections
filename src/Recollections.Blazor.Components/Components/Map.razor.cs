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
        internal protected bool IsEditable { get; set; }

        [Parameter]
        internal protected IList<LocationModel> Markers { get; set; }

        [Parameter]
        protected Action MarkersChanged { get; set; }

        internal ElementRef Container { get; set; }

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();
            await Interop.InitializeAsync(this);

            Console.WriteLine("Map.OnAfterRenderAsync");
        }

        internal void MoveMaker(int index, double latitude, double longitude, double? altitude)
        {
            LocationModel marker = Markers[index];
            marker.Latitude = latitude;
            marker.Longitude = longitude;
            marker.Altitude = altitude;
            MarkersChanged?.Invoke();
        }
    }
}
