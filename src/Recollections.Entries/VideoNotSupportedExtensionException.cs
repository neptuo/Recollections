namespace Neptuo.Recollections.Entries
{
    public class VideoNotSupportedExtensionException : VideoUploadValidationException
    {
        public VideoNotSupportedExtensionException()
            : base("Video file extension not supported")
        { }
    }
}
