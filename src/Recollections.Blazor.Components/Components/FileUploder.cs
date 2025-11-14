using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components;

public class FileUploader(FileUploadInterop interop, ILog<FileUploader> log)
{
    private bool isInitialized;
    private Dictionary<string, List<Action<FileUploadProgress[]>>> progressNotifications = [];

    public async Task<IAsyncDisposable> BindFormAsync(string entityType, string entityId, string url, ElementReference formElement, ElementReference dragAndDropContainer)
    {
        if (!isInitialized)
        {
            log.Debug("FileUploader.Initialize");
            interop.Initialize(this);
            isInitialized = true;
        }

        log.Debug("FileUploader.BindFormAsync");
        await interop.BindFormAsync(
            entityType,
            entityId,
            url,
            formElement,
            dragAndDropContainer
        );

        // TODO: Create disposable to unbind the form.
        return new AsyncDisposableAction(() => Task.CompletedTask);
    }

    internal void OnProgress(FileUploadProgress[] progresses)
    {
        log.Debug($"FileUploader.OnProgress");

        foreach (var g in progresses.GroupBy(p => $"{p.EntityType}_{p.EntityId}"))
        {
            if (progressNotifications.TryGetValue(g.Key, out var listeners))
            {
                foreach (var listener in listeners)
                    listener(g.ToArray());
            }
        };
    }

    public IDisposable AddProgressListener(string entityType, string entityId, Action<FileUploadProgress[]> listener)
    {
        string key = $"{entityType}_{entityId}";
        if (!progressNotifications.TryGetValue(key, out var listeners))
            progressNotifications[key] = listeners = [];

        listeners.Add(listener);
        return new DisposableAction(() => listeners.Remove(listener));
    }

    public Task<FileUploadToRetry[]> GetStoredFilesToRetryAsync(string entityType, string entityId)
    {
        return interop.GetStoredFilesToRetryAsync(entityType, entityId);
    }

    public Task RetryEntityQueueAsync(string entityType, string entityId)
    {
        return interop.RetryEntityQueueAsync(entityType, entityId);
    }

    public Task ClearEntityQueueAsync(string entityType, string entityId)
    {
        return interop.ClearEntityQueueAsync(entityType, entityId);
    }

    public Task DeleteFileAsync(string fileId)
    {
        return interop.DeleteFileAsync(fileId);
    }

    public Task SetBearerTokenAsync(string bearerToken)
    {
        return interop.SetBearerTokenAsync(bearerToken);
    }
}