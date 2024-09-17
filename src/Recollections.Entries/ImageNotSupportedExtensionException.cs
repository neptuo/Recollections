using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    [Serializable]
    public class ImageNotSupportedExtensionException : ImageUploadValidationException
    {
        public ImageNotSupportedExtensionException()
        { }
    }
}
