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

        public FileUploadModel Model { get; set; }

        public FileUploadInterop(IJSRuntime jsRuntime, ILog<FileUploadInterop> log)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            Ensure.NotNull(log, "log");
            this.jsRuntime = jsRuntime;
            this.log = log;
        }

        public async Task InitializeAsync(FileUploadModel model, string bearerToken)
        {
            Model = model;
            await jsRuntime.InvokeVoidAsync("FileUpload.Initialize", DotNetObjectReference.Create(this), model.FormElement, bearerToken);
        }

        [JSInvokable]
        public void OnCompleted(FileUploadProgress[] progresses)
        {
            log.Debug($"FileUploadInterop.OnCompleted");
            Model.OnCompleted(progresses);
        }
    }
}
