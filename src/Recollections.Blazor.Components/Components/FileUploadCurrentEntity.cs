using System;
using Microsoft.AspNetCore.Components;

namespace Neptuo.Recollections.Components;

public class FileUploadCurrentEntity : ComponentBase, IDisposable
{
    private IDisposable registration;
    private string previousEntityType;
    private string previousEntityId;
    private string previousUrl;

    [Inject]
    protected FileUploader FileUploader { get; set; }

    [Parameter]
    public string EntityType { get; set; }

    [Parameter]
    public string EntityId { get; set; }

    [Parameter]
    public string Url { get; set; }

    protected override void OnParametersSet()
    {
        if (previousEntityType == EntityType && previousEntityId == EntityId && previousUrl == Url)
            return;

        previousEntityType = EntityType;
        previousEntityId = EntityId;
        previousUrl = Url;

        registration?.Dispose();
        registration = null;

        if (!string.IsNullOrWhiteSpace(EntityType) && !string.IsNullOrWhiteSpace(EntityId) && !string.IsNullOrWhiteSpace(Url))
            registration = FileUploader.RegisterCurrentEntity(EntityType, EntityId, Url);
    }

    public void Dispose()
    {
        registration?.Dispose();
    }
}
