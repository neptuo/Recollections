using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class FileUploadProgress
    {
        public int Total { get; }
        public int Completed { get; }

        public FileUploadProgress(int total, int completed)
        {
            Ensure.NotNull(total, "total");
            Ensure.NotNull(completed, "completed");
            Total = total;
            Completed = completed;
        }
    }
}
