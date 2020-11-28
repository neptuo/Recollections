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
    /// An exception raised when free limits has been reached.
    /// </summary>
    [Serializable]
    public class FreeLimitsReachedExceptionException : Exception
    {
        /// <summary>
        /// Creates new empty instance.
        /// </summary>
        public FreeLimitsReachedExceptionException()
        { }

        /// <summary>
        /// Creates new instance with the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The text description of the problem.</param>
        public FreeLimitsReachedExceptionException(string message)
            : base(message)
        { }

        /// <summary>
        /// Creates new instance with the <paramref name="message"/> and <paramref name="inner"/> exception.
        /// </summary>
        /// <param name="message">The text description of the problem.</param>
        /// <param name="inner">The inner cause of the exceptional state.</param>
        public FreeLimitsReachedExceptionException(string message, Exception inner)
            : base(message, inner)
        { }

        /// <summary>
        /// Creates new instance for deserialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected FreeLimitsReachedExceptionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
