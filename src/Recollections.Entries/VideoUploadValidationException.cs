using System;

namespace Neptuo.Recollections.Entries
{
    public class VideoUploadValidationException : Exception
    {
        public VideoUploadValidationException(string message = null)
            : base(message)
        { }

        public VideoUploadValidationException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
