using Neptuo;
using Neptuo.Recollections.Entries;
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
        public string Name { get; set; }

        public string Status { get; set; }
        public int StatusCode { get; set; }
        public string ResponseText { get; set; }

        public int Size { get; set; }
        public int Uploaded { get; set; }

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
}
