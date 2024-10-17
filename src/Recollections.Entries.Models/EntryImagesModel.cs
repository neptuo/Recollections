using System.Collections.Generic;

namespace Neptuo.Recollections.Entries;

public class EntryImagesModel
{
    public string EntryId { get; set; }
    public List<ImageModel> Images { get; set; } = new();
}