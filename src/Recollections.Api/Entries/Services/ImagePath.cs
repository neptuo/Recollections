using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Services
{
    internal class ImagePath
    {
        public string Original { get; }
        public string Preview { get; }
        public string Thumbnail { get; }

        public ImagePath(SystemIoFileStorage storage, ImageResizeService service, Entry entry, Image image)
        {
            Ensure.NotNull(service, "service");
            Ensure.NotNull(entry, "entry");
            Ensure.NotNull(image, "image");

            string storagePath = storage.GetStoragePath(entry);
            string baseName = Path.GetFileNameWithoutExtension(image.FileName);

            Original = Path.Combine(storagePath, image.FileName);
            Thumbnail = Path.Combine(storagePath, String.Concat(baseName, ".thumbnail", service.ImageExtension));
            Preview = Path.Combine(storagePath, String.Concat(baseName, ".preview", service.ImageExtension));
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
