using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class ImageFormatDefinition
    {
        public static readonly ImageFormatDefinition Jpeg = new ImageFormatDefinition(".jpg");

        public ImageEncoder Codec { get; }
        public string FileExtension { get; }

        public ImageFormatDefinition(string fileExtension)
        {
            Ensure.NotNullOrEmpty(fileExtension, "fileExtension");
            FileExtension = fileExtension;

            if (fileExtension == ".jpg")
            {
                Codec = new JpegEncoder()
                {
                    Quality = 50,
                };
            }
            else
            {
                throw Ensure.Exception.NotSupported($"The extension '{fileExtension}' is not supported image format.");
            }
        }
    }
}
