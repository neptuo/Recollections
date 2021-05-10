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
        public const string DefaultText = "Upload Images";

        [Inject]
        protected FileUploadInterop Interop { get; set; }

        [Inject]
        protected ILog<FileUpload> Log { get; set; }

        [Parameter]
        public string Text { get; set; } = DefaultText;

        [Parameter]
        public string Url { get; set; }

        [Parameter]
        public string BearerToken { get; set; }

        [Parameter]
        public Action<IReadOnlyCollection<FileUploadProgress>> Progress { get; set; }

        [Parameter]
        public ElementReference DragAndDropContainer { get; set; }

        public ElementReference FormElement { get; protected set; }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            Log.Debug("FileUploadModel.OnAfterRenderAsync");

            await base.OnAfterRenderAsync(firstRender);
            await Interop.InitializeAsync(this, BearerToken);
        }

        internal void OnCompleted(FileUploadProgress[] progresses)
        {
            Log.Debug("FileUploadModel.OnCompleted");
            Progress?.Invoke(progresses);
        }

        public async ValueTask DisposeAsync()
        {
            await Interop.DestroyAsync();
        }
    }
}
