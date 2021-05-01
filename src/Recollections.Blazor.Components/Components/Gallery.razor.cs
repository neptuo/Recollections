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
        [Inject]
        protected GalleryInterop Interop { get; set; }

        [Parameter]
        public List<GalleryModel> Models { get; set; }

        [Parameter]
        public Func<int, Task<string>> DataGetter { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (Models != null && Models.Count > 0)
                await Interop.InitializedAsync(this, Models);
        }

        public void Open(int index)
        {
            _ = Interop.OpenAsync(index);
        }
    }
}
