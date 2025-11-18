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

        public const string DefaultText = "Upload Images";

        [Inject]
        protected FileUploader FileUploader { get; set; }

        [Inject]
        protected ILog<FileUpload> Log { get; set; }

        [Inject]
        protected IFreeLimitsNotifier FreeLimitsNotifier { get; set; }

        private IDisposable previousUploadListener;
        private string previousEntityType;
        private string previousEntityId;

        [Parameter]
        public string Text { get; set; } = DefaultText;

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
        protected Modal UploadRetryModal { get; set; }

        protected List<FileUploadProgress> UploadErrors { get; } = [];
        protected List<FileUploadToRetry> UploadsToRetry { get; } = [];

        public override Task SetParametersAsync(ParameterView parameters)
        {
            previousEntityType = EntityType;
            previousEntityId = EntityId;
            return base.SetParametersAsync(parameters);
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (previousEntityType != EntityType || previousEntityId != EntityId)
            {
                var storedFiles = await FileUploader.GetStoredFilesToRetryAsync(EntityType, EntityId);
                if (storedFiles.Length > 0)
                    OnStoredFilesDetected(storedFiles);

                previousUploadListener?.Dispose();
                previousUploadListener = FileUploader.AddProgressListener(EntityType, EntityId, OnUploadProgress);
            }
        }

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

        protected void OnStoredFilesDetected(IReadOnlyCollection<FileUploadToRetry> retries)
        {
            Log.Debug($"OnStoredFilesDetected '{retries.Count}' files");
            UploadsToRetry.Clear();
            UploadsToRetry.AddRange(retries);
            UploadRetryModal.Show();
        }

        protected void OnUploadProgress(IReadOnlyCollection<FileUploadProgress> progresses)
        {
            Log.Debug($"OnUploadProgressAsync '{progresses.Count}' files");

            if (progresses.All(p => p.Status == "done" || p.Status == "error"))
            {
                UploadErrors.Clear();
                UploadErrors.AddRange(progresses.Where(p => p.Status == "error"));
                if (UploadErrors.Count > 0)
                {
                    if (UploadErrors.All(e => e.StatusCode == 402))
                        FreeLimitsNotifier.Show();
                    else
                        UploadError.Show();
                }
            }

            StateHasChanged();
        }

        protected async Task DeleteUploadToRetryAsync(FileUploadToRetry retry)
        {
            await FileUploader.DeleteFileAsync(retry.Id);
            UploadsToRetry.Remove(retry);

            if (UploadsToRetry.Count == 0)
                UploadRetryModal.Hide();
                
            StateHasChanged();
        }
    }
}
