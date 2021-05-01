using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DrImage = System.Drawing.Image;

namespace Neptuo.Recollections.Entries
{
    public class ImageResizeService
    {
        private readonly ImageFormatDefinition formatDefinition;

        public string ImageExtension => formatDefinition.FileExtension;

        public ImageResizeService(ImageFormatDefinition formatDefinition)
        {
            Ensure.NotNull(formatDefinition, "formatDefinition");
            this.formatDefinition = formatDefinition;
        }

        public void Thumbnail(Stream inputContent, Stream outputContent, int width, int height)
        {
            using (var input = DrImage.FromStream(inputContent))
            {
                EnsureExifImageRotation(input, inputContent);

                int sourceWidth = input.Width;
                int sourceHeight = input.Height;

                double widthRatio = (double)sourceWidth / width;
                double heightRatio = (double)sourceHeight / height;

                double ratio = widthRatio < heightRatio ? widthRatio : heightRatio;

                sourceWidth = (int)(ratio * width);
                sourceHeight = (int)(ratio * height);

                int offsetX = (input.Width - sourceWidth) / 2;
                int offsetY = (input.Height - sourceHeight) / 2;

                Resize(input, outputContent, new Rectangle(offsetX, offsetY, sourceWidth, sourceHeight), width, height);
            }
        }

        public (int width, int height) GetResizedBounds(int originalWidth, int originalHeight, int desiredWidth)
        {
            if (desiredWidth < originalWidth)
            {
                var ratio = desiredWidth / (double)originalWidth;
                int desiredHeight = (int)(ratio * originalHeight);

                return (desiredWidth, desiredHeight);
            }

            return (originalWidth, originalHeight);
        }

        public (int width, int height) GetSize(Stream inputContent)
        {
            using var input = DrImage.FromStream(inputContent);
            return (input.Width, input.Height);
        }

        public void Resize(Stream inputContent, Stream outputContent, int desiredWidth)
        {
            using (var input = DrImage.FromStream(inputContent))
            {
                EnsureExifImageRotation(input, inputContent);

                var (width, height) = GetResizedBounds(input.Width, input.Height, desiredWidth);
                if (width != input.Width)
                    Resize(input, outputContent, null, width, height);
                else
                    SaveImage(outputContent, input);
            }
        }

        private void SaveImage(Stream outputContent, DrImage target) 
            => target.Save(outputContent, formatDefinition.Codec, formatDefinition.EncoderParameters);

        private void Resize(DrImage input, Stream outputContent, Rectangle? inputRect, int width, int height)
        {
            using (var target = new Bitmap(width, height))
            using (Graphics targetGraphics = Graphics.FromImage(target))
            {
                targetGraphics.CompositingQuality = CompositingQuality.HighQuality;
                targetGraphics.SmoothingMode = SmoothingMode.HighQuality;
                targetGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                targetGraphics.DrawImage(input, new Rectangle(0, 0, width, height), inputRect ?? new Rectangle(0, 0, input.Width, input.Height), GraphicsUnit.Pixel);

                SaveImage(outputContent, target);
            }
        }

        private void EnsureExifImageRotation(DrImage image, Stream imageContent)
        {
            imageContent.Position = 0;
            using (var imageReader = new ImagePropertyReader(imageContent))
            {
                ImagePropertyReader.Orientation? orientation = imageReader.FindOrientation();
                if (orientation != null)
                {
                    switch (orientation.Value)
                    {
                        case ImagePropertyReader.Orientation.D270:
                            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            break;
                        case ImagePropertyReader.Orientation.D180:
                            image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            break;
                        case ImagePropertyReader.Orientation.D90:
                            image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            break;
                    }
                }
            }
        }
    }
}
