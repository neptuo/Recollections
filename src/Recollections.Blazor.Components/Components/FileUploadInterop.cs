using Microsoft.JSInterop;
using Neptuo;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class FileUploadInterop
    {
        private readonly IJSRuntime js;
        private IJSObjectReference module;
        private readonly ILog<FileUploadInterop> log;

        public FileUpload Editor { get; set; }

        public FileUploadInterop(IJSRuntime js, ILog<FileUploadInterop> log)
        {
            Ensure.NotNull(js, "js");
            Ensure.NotNull(log, "log");
            this.js = js;
            this.log = log;
        }

        public async Task InitializeAsync(FileUpload editor, string bearerToken, string entityType, string entityId)
        {
            Editor = editor;

            module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/FileUpload.js");
            await module.InvokeVoidAsync(
                "initialize",
                DotNetObjectReference.Create(this),
                editor.FormElement,
                bearerToken,
                editor.DragAndDropContainer,
                entityType,
                entityId
            );
        }

        public async Task DestroyAsync()
        {
            await module.InvokeVoidAsync("destroy");
        }

        [JSInvokable("FileUpload.OnCompleted")]
        public void OnCompleted(FileUploadProgress[] progresses)
        {
            log.Debug($"FileUploadInterop.OnCompleted");
            Editor.OnCompleted(progresses);
        }
    }
}
