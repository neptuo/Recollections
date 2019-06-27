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
        private static Dictionary<string, FileUploadModel> models = new Dictionary<string, FileUploadModel>();

        public FileUploadInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public Task InitializeAsync(FileUploadModel model, string bearerToken)
        {
            Console.WriteLine($"Add file upload form {model.FormId}");
            if (!models.ContainsKey(model.FormId))
            {
                models.Add(model.FormId, model);
                return jsRuntime.InvokeAsync<bool>("FileUpload.Initialize", model.FormId, bearerToken);
            }

            return Task.CompletedTask;
        }

        [JSInvokable]
        public static void FileUpload_OnCompleted(string id)
        {
            Console.WriteLine($"FileUpload_OnCompleted, FormId: {id}");
            if (models.TryGetValue(id, out FileUploadModel model))
            {
                Console.WriteLine("Model found");
                model.OnCompleted();
            }
        }

        public void Destroy(FileUploadModel model)
        {
            Console.WriteLine($"Destroy file upload form {model.FormId}");
            models.Remove(model.FormId);
        }
    }
}
