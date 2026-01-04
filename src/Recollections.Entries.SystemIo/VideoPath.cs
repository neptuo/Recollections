using System.IO;

namespace Neptuo.Recollections.Entries
{
    public class VideoPath
    {
        public string Original { get; }
        public string Thumbnail { get; }

        public VideoPath(SystemIoFileStorage storage, ImageFormatDefinition formatDefinition, Entry entry, Video video)
        {
            Ensure.NotNull(formatDefinition, "formatDefinition");
            Ensure.NotNull(entry, "entry");
            Ensure.NotNull(video, "video");

            string storagePath = storage.GetStoragePath(entry);
            string baseName = Path.GetFileNameWithoutExtension(video.FileName);

            Original = Path.Combine(storagePath, video.FileName);
            Thumbnail = Path.Combine(storagePath, string.Concat(baseName, ".thumbnail", formatDefinition.FileExtension));
        }

        public string Get(VideoType type)
        {
            switch (type)
            {
                case VideoType.Original:
                    return Original;
                case VideoType.Thumbnail:
                    return Thumbnail;
                default:
                    throw Ensure.Exception.NotSupported(type);
            }
        }
    }
}
