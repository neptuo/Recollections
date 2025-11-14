using System;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Neptuo.Logging;

namespace Neptuo.Recollections.Components;

public class FileUploadProgressListener(FileUploader fileUploader, ILog<FileUploadProgressListener> log) : ComponentBase, IDisposable
{
    private IDisposable listenerRegistration;
    private FileUploadProgress[] files;

    [Parameter]
    public RenderFragment<FileUploadProgress[]> ChildContent { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        listenerRegistration = fileUploader.AddProgressListener(OnProgress);
    }

    public void Dispose()
    {
        listenerRegistration?.Dispose();
    }

    private void OnProgress(FileUploadProgress[] files)
    {
        log.Debug($"OnProgress '{files?.Length}' files");

        if (files.All(p => p.Status == "done" || p.Status == "error"))
        {
            log.Debug("All files 'done' or 'error'");
            files = [];
        }

        this.files = files;
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent != null)
            builder.AddContent(0, ChildContent(files ?? []));
    }
}