using Microsoft.AspNetCore.Components;
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
        private bool isInitialized = false;
        private readonly ILog<FileUploadInterop> log;
        private FileUploader uploader;

        private string userId;
        private string bearerToken;

        public FileUploadInterop(IJSRuntime js, ILog<FileUploadInterop> log)
        {
            Ensure.NotNull(js, "js");
            Ensure.NotNull(log, "log");
            this.js = js;
            this.log = log;
        }

        private async Task EnsureModuleAsync()
        {
            if (module == null)
                module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Recollections.Blazor.Components/FileUpload.js");
        }

        public void Initialize(FileUploader uploader)
            => this.uploader = uploader;

        public async Task BindFormAsync(string entityType, string entityId, string url, ElementReference formElement, ElementReference dragAndDropContainer)
        {
            await EnsureModuleAsync();
            
            if (!isInitialized)
            {
                isInitialized = true;
                await module.InvokeVoidAsync(
                    "initialize",
                    DotNetObjectReference.Create(this)
                );

                if (!string.IsNullOrEmpty(bearerToken))
                    await SetBearerTokenAsync(userId, bearerToken);
            }

            await module.InvokeVoidAsync(
                "bindForm",
                entityType,
                entityId,
                url,
                formElement,
                dragAndDropContainer
            );
        }

        [JSInvokable("FileUpload.OnChange")]
        public void OnChange(FileUploadProgress[] progresses)
        {
            log.Debug($"FileUploadInterop.OnChange");
            uploader.OnChange(progresses);
        }

        [JSInvokable("FileUpload.OnProgress")]
        public void OnProgress(int index, int total, int loaded)
        {
            log.Debug($"FileUploadInterop.OnProgress");
            uploader.OnProgress(index, total, loaded);
        }

        public async Task<FileUploadToRetry[]> GetStoredFilesToRetryAsync(string entityType, string entityId)
        {
            await EnsureModuleAsync();
            return await module.InvokeAsync<FileUploadToRetry[]>("getEntityStoredFiles", entityType, entityId);
        }

        public async Task RetryEntityQueueAsync(string entityType, string entityId)
        {
            await EnsureModuleAsync();
            await module.InvokeVoidAsync("retryEntityQueue", entityType, entityId);
        }

        public async Task ClearEntityQueueAsync(string entityType, string entityId)
        {
            await EnsureModuleAsync();
            await module.InvokeVoidAsync("clearEntityQueue", entityType, entityId);
        }

        public async Task DeleteFileAsync(string fileId)
        {
            await EnsureModuleAsync();
            await module.InvokeVoidAsync("deleteFile", fileId);
        }

        public async Task DestroyAsync()
        {
            await EnsureModuleAsync();
            await module.InvokeVoidAsync("destroy");
        }

        internal async Task SetBearerTokenAsync(string userId, string bearerToken)
        {
            if (module == null)
            {
                this.userId = userId;
                this.bearerToken = bearerToken;
            }
            else
            {
                await module.InvokeVoidAsync("setBearerToken", userId, bearerToken);
            }
        }
    }
}
