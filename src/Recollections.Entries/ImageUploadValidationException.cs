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
    public class ImageUploadValidationException : Exception
    {
        protected ImageUploadValidationException()
        { }

        /// <summary>
        /// Creates a new instance for deserialization.
        /// </summary>
        /// <param name="info">A serialization info.</param>
        /// <param name="context">A streaming context.</param>
        protected ImageUploadValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context) 
        { }
    }
}
