using System;

namespace Neptuo.Recollections.Components;

public record FileUploadToRetry(string Name, int Size, string Id, string ContentType = null, string PreviewUrl = null)
{
    public bool IsImage => ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
    public bool IsVideo => ContentType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true;
    public bool HasPreview => !string.IsNullOrEmpty(PreviewUrl) && (IsImage || IsVideo);
}
