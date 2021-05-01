using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class ImagePath
    {
        public string Original { get; }
        public string Preview { get; }
        public string Thumbnail { get; }

        public ImagePath(SystemIoFileStorage storage, ImageFormatDefinition formatDefinition, Entry entry, Image image)
        {
            Ensure.NotNull(formatDefinition, "formatDefinition");
            Ensure.NotNull(entry, "entry");
            Ensure.NotNull(image, "image");

            string storagePath = storage.GetStoragePath(entry);
            string baseName = Path.GetFileNameWithoutExtension(image.FileName);

            Original = Path.Combine(storagePath, image.FileName);
            Thumbnail = Path.Combine(storagePath, String.Concat(baseName, ".thumbnail", formatDefinition.FileExtension));
            Preview = Path.Combine(storagePath, String.Concat(baseName, ".preview", formatDefinition.FileExtension));
        }

        public string Get(ImageType type)
        {
            switch (type)
            {
                case ImageType.Original:
                    return Original;
                case ImageType.Preview:
                    return Preview;
                case ImageType.Thumbnail:
                    return Thumbnail;
                default:
                    throw Ensure.Exception.NotSupported(type);
            }
        }
    }
}
