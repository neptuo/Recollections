namespace Neptuo.Recollections.Entries
{
    public class VideoMaxLengthExceededException : VideoUploadValidationException
    {
        public VideoMaxLengthExceededException()
            : base("Video file too large")
        { }
    }
}
