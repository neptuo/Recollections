using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class FileUploadError : FileUploadProgress
    {
        public int StatusCode { get; }

        public FileUploadError(int statusCode, int total, int completed) 
            : base(total, completed)
        {
            StatusCode = statusCode;
        }
    }
}
