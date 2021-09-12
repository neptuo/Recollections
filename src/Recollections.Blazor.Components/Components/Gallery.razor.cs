using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class Gallery
    {
        private int? indexToOpen;

        [Inject]
        protected GalleryInterop Interop { get; set; }

        [Parameter]
        public List<GalleryModel> Models { get; set; }

        [Parameter]
        public Func<int, Task<string>> DataGetter { get; set; }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await Interop.InitializedAsync(this, Models ?? new List<GalleryModel>(0));

            if (indexToOpen != null)
            {
                await Interop.OpenAsync(indexToOpen.Value);
                indexToOpen = null;
            }
        }

        public void Open(int index)
        {
            indexToOpen = index;
        }
    }
}
