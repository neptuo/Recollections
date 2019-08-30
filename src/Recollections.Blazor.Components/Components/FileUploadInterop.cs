using Microsoft.JSInterop;
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

        public FileUploadModel Model { get; set; }

        public FileUploadInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public Task InitializeAsync(FileUploadModel model, string bearerToken)
        {
            Model = model;
            return jsRuntime.InvokeAsync<object>("FileUpload.Initialize", DotNetObjectRef.Create(this), model.FormElement, bearerToken);
        }

        [JSInvokable]
        public void OnCompleted(FileUploadProgress[] progresses)
        {
            Console.WriteLine($"FileUploadInterop.OnCompleted");
            Model.OnCompleted(progresses);
        }
    }
}
