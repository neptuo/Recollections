using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrImage = System.Drawing.Image;

namespace Neptuo.Recollections.Entries.Services
{
    public class ImageResizeService
    {
        private const int ImageRotationPropertyId = 0x112;

        private ImageFormat ImageFormat => ImageFormat.Png;

        public void Thumbnail(string inputPath, string outputPath, int width, int height)
        {
            using (var input = DrImage.FromFile(inputPath))
            {
                EnsureExifImageRotation(input);

                int sourceWidth = input.Width;
                int sourceHeight = input.Height;

                double widthRatio = (double)sourceWidth / width;
                double heightRatio = (double)sourceHeight / height;

                double ratio = widthRatio < heightRatio ? widthRatio : heightRatio;

                sourceWidth = (int)(ratio * width);
                sourceHeight = (int)(ratio * height);

                int offsetX = (input.Width - sourceWidth) / 2;
                int offsetY = (input.Height - sourceHeight) / 2;

                Resize(input, outputPath, new Rectangle(offsetX, offsetY, sourceWidth, sourceHeight), width, height);
            }
        }

        public void Resize(string inputPath, string outputPath, int width)
        {
            using (var input = DrImage.FromFile(inputPath))
            {
                EnsureExifImageRotation(input);

                if (width < input.Width)
                {
                    var ratio = width / (double)input.Width;
                    int height = (int)(ratio * input.Height);

                    Resize(input, outputPath, null, width, height);
                }
                else
                {
                    input.Save(outputPath, ImageFormat);
                }
            }
        }

        private void Resize(DrImage input, string outputPath, Rectangle? inputRect, int width, int height)
        {
            using (var target = new Bitmap(width, height))
            using (Graphics targetGraphics = Graphics.FromImage(target))
            {
                targetGraphics.CompositingQuality = CompositingQuality.HighQuality;
                targetGraphics.SmoothingMode = SmoothingMode.HighQuality;
                targetGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                targetGraphics.DrawImage(input, new Rectangle(0, 0, width, height), inputRect ?? new Rectangle(0, 0, input.Width, input.Height), GraphicsUnit.Pixel);

                target.Save(outputPath, ImageFormat);
            }
        }

        private void EnsureExifImageRotation(DrImage image)
        {
            if (image.PropertyIdList.Contains(ImageRotationPropertyId))
            {
                PropertyItem property = image.GetPropertyItem(ImageRotationPropertyId);
                if (property.Type == 3 && property.Len == 2)
                {
                    ushort orientationExif = BitConverter.ToUInt16(image.GetPropertyItem(ImageRotationPropertyId).Value, 0);
                    if (orientationExif == 8)
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    else if (orientationExif == 3)
                        image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    else if (orientationExif == 6)
                        image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                }
            }
        }
    }
}
