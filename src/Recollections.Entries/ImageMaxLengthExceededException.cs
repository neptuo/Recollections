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
    public class ImageMaxLengthExceededException : ImageUploadValidationException
    {
        public ImageMaxLengthExceededException()
        { }

        /// <summary>
        /// Creates a new instance for deserialization.
        /// </summary>
        /// <param name="info">A serialization info.</param>
        /// <param name="context">A streaming context.</param>
        protected ImageMaxLengthExceededException(SerializationInfo info, StreamingContext context)
            : base(info, context) 
        { }
    }
}
