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
    public class FileUploadModel : ComponentBase
    {
        public const string DefaultText = "Upload Images";

        [Inject]
        protected FileUploadInterop Interop { get; set; }

        [Inject]
        protected ILog<FileUploadModel> Log { get; set; }

        [Parameter]
        protected string Text { get; set; } = DefaultText;

        [Parameter]
        protected string Url { get; set; }

        [Parameter]
        protected string BearerToken { get; set; }

        [Parameter]
        protected Action<IReadOnlyCollection<FileUploadProgress>> Progress { get; set; }

        public ElementRef FormElement { get; protected set; }

        protected async override Task OnAfterRenderAsync()
        {
            Log.Debug("FileUploadModel.OnAfterRenderAsync");

            await base.OnAfterRenderAsync();
            await Interop.InitializeAsync(this, BearerToken);
        }

        internal void OnCompleted(FileUploadProgress[] progresses)
        {
            Log.Debug("FileUploadModel.OnCompleted");
            Progress?.Invoke(progresses);
        }
    }
}
