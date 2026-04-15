using Microsoft.AspNetCore.Components;
using Neptuo.Identifiers;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class FileUpload : ComponentBase, IAsyncDisposable
    {
        private IAsyncDisposable formBinding;

        public const string DefaultText = "Upload Media";

        [Inject]
        protected FileUploader FileUploader { get; set; }

        [Inject]
        protected ILog<FileUpload> Log { get; set; }

        [CascadingParameter]
        protected IItemContainer Container { get; set; }

        [Parameter]
        public string Text { get; set; } = DefaultText;

        [Parameter]
        public string ButtonCssClass { get; set; } = "btn btn-sm btn-primary";

        [Parameter]
        public string FormCssClass { get; set; }

        [Parameter]
        public string EntityType { get; set; }

        [Parameter]
        public string EntityId { get; set; }

        [Parameter]
        public string Url { get; set; }

        [Parameter]
        public ElementReference DragAndDropContainer { get; set; }

        internal ElementReference FormElement { get; private set; }

        protected Modal UploadError { get; set; }
        protected List<FileUploadProgress> UploadErrors { get; } = [];

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            Log.Debug("OnAfterRenderAsync");

            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
                formBinding = await FileUploader.BindFormAsync(EntityType, EntityId, Url, FormElement, DragAndDropContainer);
        }

        public async ValueTask DisposeAsync()
        {
            if (formBinding != null)
                await formBinding.DisposeAsync();
        }

        private void OnClick()
            => Container?.Close();
    }
}
