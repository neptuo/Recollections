using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class ImageFormatDefinition
    {
        public static readonly ImageFormatDefinition Jpeg = new ImageFormatDefinition(ImageFormat.Jpeg, ".jpg");

        public ImageFormat Format { get; }
        public ImageCodecInfo Codec { get; }
        public string FileExtension { get; }
        public EncoderParameters EncoderParameters { get; }

        public ImageFormatDefinition(ImageFormat format, string fileExtension)
        {
            Ensure.NotNull(format, "format");
            Ensure.NotNullOrEmpty(fileExtension, "fileExtension");
            Format = format;
            Codec = GetCodecInfo(format);
            FileExtension = fileExtension;

            EncoderParameters = new EncoderParameters(3);
            EncoderParameters.Param[0] = new EncoderParameter(Encoder.ColorDepth, 8L);
            EncoderParameters.Param[1] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);
            EncoderParameters.Param[2] = new EncoderParameter(Encoder.Quality, 50L);
        }

        private static ImageCodecInfo GetCodecInfo(ImageFormat imageFormat)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].FormatID == imageFormat.Guid)
                    return encoders[j];
            }

            return null;
        }
    }
}
