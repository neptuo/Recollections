using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class Gallery(ILog<Gallery> Log, GalleryInterop Interop) : IAsyncDisposable
    {
        private int previousParametersHashCode;

        [Parameter]
        public List<GalleryModel> Models { get; set; }

        [Parameter]
        public string BearerToken { get; set; }

        [Parameter]
        public EventCallback<int> OnOpenInfo { get; set; }

        private int ComputeParametersHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(Models ?? []);
            if (Models != null)
            {
                hashCode.Add(Models.Count);
                foreach (var model in Models)
                    hashCode.Add(model);
            }

            hashCode.Add(BearerToken);
            return hashCode.ToHashCode();
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await Interop.InitializedAsync(this, Models ?? [], BearerToken);

            // 'Models' is passed as reference, so when SetParametersAsync is called, it already contains a new items.
            // Because of that it's important we compute the 'previous' value at the end of the rendering cycle,
            // because there is no place in the beginning.
            previousParametersHashCode = ComputeParametersHashCode();
        }

        protected override bool ShouldRender()
        {
            var result = previousParametersHashCode != ComputeParametersHashCode();
            Log.Debug($"ShouldRender: result = '{result}'");
            return result;
        }

        protected async Task OnBeforeInternalNavigation(LocationChangingContext context)
        {
            if (await IsOpenAsync())
            {
                _ = CloseAsync();
                context.PreventNavigation();
            }
        }

        public async Task OpenAsync(int index)
        {
            if (Models.Count > index)
            {
                await Interop.InitializedAsync(this, Models ?? [], BearerToken);
                await Interop.OpenAsync(index);
            }
        }

        public Task CloseAsync()
            => Interop.CloseAsync();

        public Task<bool> IsOpenAsync()
            => Interop.IsOpenAsync();

        public async ValueTask DisposeAsync()
            => await Interop.DisposeAsync();
    }
}
