using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    /// <summary>
    /// An exception raised when the API application update is in progress
    /// </summary>
    [Serializable]
    public class ServiceUnavailableException : Exception
    {
        /// <summary>
        /// Creates new empty instance.
        /// </summary>
        public ServiceUnavailableException()
        { }

        /// <summary>
        /// Creates new instance for deserialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected ServiceUnavailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
