using System.Collections.Generic;

namespace Neptuo.Recollections.Sharing;

public class ShareRootModel
{
    public bool IsInherited { get; set; }
    public List<ShareModel> Models { get; set; }

    public ShareRootModel()
    { }

    public ShareRootModel(bool isInherited, List<ShareModel> models)
    {
        IsInherited = isInherited;
        Models = models;
    }
}