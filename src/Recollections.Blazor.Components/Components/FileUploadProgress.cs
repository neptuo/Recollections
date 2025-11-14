using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components;

public record FileUploadProgress(
    string Id,
    string EntityType,
    string EntityId,
    string Name,
    string Status,
    int StatusCode,
    string ResponseText,
    int Size,
    int Uploaded
)
{
    public int Percentual
    {
        get
        {
            int percentual = 0;
            if (Size > 0)
                percentual = (int)Math.Floor(Uploaded / (decimal)Size * 100);

            return percentual;
        }
    }
}
