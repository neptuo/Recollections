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
    public int Uploaded { get; internal set; } = Uploaded;

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

    public object Tag { get; set; }

    public bool IsDone => Status == "done";
    public bool IsError => Status == "error";
    public bool IsPending => Status == "pending";
    public bool IsCurrent => Status == "current";
    
    public string StatusDescription
    {
        get
        {
            if (Status == "done")
                return "Uploaded";
            else if (IsCurrent && Percentual == 100)
                return $"Saving...";
            else if (IsCurrent)
                return $"{Percentual}%";
            else if (IsError)
                return "Failed to upload";
            else if (IsPending)
                return "Waiting";
            else
                return "Unknown...";
        }
    }
    
    public string ErrorDescription
    {
        get
        {
            if (StatusCode == 400)
                return "File is too large.";
            else if (StatusCode == 402)
                return "Premium required.";
            else
                return "Unexpected server error.";
        }
    }
}
