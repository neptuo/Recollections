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
    private FileUploadProgress[] lastProgresses;
    private List<Action<FileUploadProgress[]>> progressNotifications = [];
    private Dictionary<string, List<Action<FileUploadProgress[]>>> progressNotificationsPerEntity = [];

    public async Task<IAsyncDisposable> BindFormAsync(string entityType, string entityId, string url, ElementReference formElement, ElementReference dragAndDropContainer)
    {
        if (!isInitialized)
        {
            log.Debug("Initialize");
            interop.Initialize(this);
            isInitialized = true;
        }

        log.Debug("BindFormAsync");
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

    private void RaiseProgressNotification(FileUploadProgress[] progresses)
    {
        foreach (var listener in progressNotifications)
            listener(progresses);

        foreach (var g in progresses.GroupBy(p => $"{p.EntityType}_{p.EntityId}"))
        {
            if (progressNotificationsPerEntity.TryGetValue(g.Key, out var listeners))
            {
                foreach (var listener in listeners)
                    listener(g.ToArray());
            }
        }
    }

    internal void OnChange(FileUploadProgress[] progresses)
    {
        log.Debug($"OnChange '{progresses.Length}' files");
        lastProgresses = progresses;
        RaiseProgressNotification(progresses);
    }

    internal void OnProgress(int index, int total, int loaded)
    {
        log.Debug($"OnProgress '{index}: {total}' loaded '{loaded}' bytes");
        if (lastProgresses == null)
            return;

        if (lastProgresses.Length > index)
            lastProgresses[index].Uploaded = loaded;
        
        RaiseProgressNotification(lastProgresses);
    }

    public IDisposable AddProgressListener(Action<FileUploadProgress[]> listener)
    {
        progressNotifications.Add(listener);
        log.Debug($"AddProgressListener global contains '{progressNotifications.Count}' listeners");

        return new DisposableAction(() =>
        {
            progressNotifications.Remove(listener);
            log.Debug($"RemoveProgressListener global remaining '{progressNotifications.Count}' listeners");
        });
    }

    public IDisposable AddProgressListener(string entityType, string entityId, Action<FileUploadProgress[]> listener)
    {
        string key = $"{entityType}_{entityId}";
        if (!progressNotificationsPerEntity.TryGetValue(key, out var listeners))
            progressNotificationsPerEntity[key] = listeners = [];

        listeners.Add(listener);
        log.Debug($"AddProgressListener '{key}' contains '{listeners.Count}' listeners");
        return new DisposableAction(() =>
        {
            listeners.Remove(listener);
            log.Debug($"RemoveProgressListener '{key}' remaining '{listeners.Count}' listeners");
        });
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

    public Task SetBearerTokenAsync(string userId, string bearerToken)
    {
        return interop.SetBearerTokenAsync(userId, bearerToken);
    }
}