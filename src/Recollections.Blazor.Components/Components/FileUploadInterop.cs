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
        private readonly IJSRuntime jsRuntime;
        private readonly ILog<FileUploadInterop> log;

        public FileUpload Editor { get; set; }

        public FileUploadInterop(IJSRuntime jsRuntime, ILog<FileUploadInterop> log)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            Ensure.NotNull(log, "log");
            this.jsRuntime = jsRuntime;
            this.log = log;
        }

        public async Task InitializeAsync(FileUpload editor, string bearerToken)
        {
            Editor = editor;
            await jsRuntime.InvokeVoidAsync("FileUpload.Initialize", DotNetObjectReference.Create(this), editor.FormElement, bearerToken);
        }

        [JSInvokable("FileUpload.OnCompleted")]
        public void OnCompleted(FileUploadProgress[] progresses)
        {
            log.Debug($"FileUploadInterop.OnCompleted");
            Editor.OnCompleted(progresses);
        }
    }
}
