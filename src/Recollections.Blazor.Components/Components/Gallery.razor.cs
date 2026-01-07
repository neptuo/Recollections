using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class Gallery : IAsyncDisposable
    {
        private int? indexToOpen;

        [Inject]
        protected GalleryInterop Interop { get; set; }

        [Parameter]
        public List<GalleryModel> Models { get; set; }

        [Parameter]
        public Func<int, string, Task<Stream>> DataGetter { get; set; }

        [Parameter]
        public EventCallback<int> OnOpenInfo { get; set; }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await Interop.InitializedAsync(this, Models ?? new List<GalleryModel>(0));

            if (indexToOpen != null)
            {
                if (Models.Count > indexToOpen.Value)
                    await Interop.OpenAsync(indexToOpen.Value);

                indexToOpen = null;
            }
        }

        protected async Task OnBeforeInternalNavigation(LocationChangingContext context)
        {
            if (await IsOpenAsync())
            {
                _ = CloseAsync();
                context.PreventNavigation();
            }
        }

        public void Open(int index) 
            => indexToOpen = index;

        public Task CloseAsync()
            => Interop.CloseAsync();

        public Task<bool> IsOpenAsync()
            => Interop.IsOpenAsync();

        public async ValueTask DisposeAsync() 
            => await Interop.CloseAsync();
    }
}
