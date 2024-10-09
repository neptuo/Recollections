using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using static System.Net.Mime.MediaTypeNames;
using IsImage = SixLabors.ImageSharp.Image;

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
            using (var input = IsImage.Load(inputContent))
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
            using var input = IsImage.Load(inputContent);

            inputContent.Position = 0;
            using var imageReader = new ImagePropertyReader(inputContent);

            int width = input.Width;
            int height = input.Height;

            ImagePropertyReader.Orientation? orientation = imageReader.FindOrientation();
            if (orientation != null)
            {
                switch (orientation.Value)
                {
                    case ImagePropertyReader.Orientation.D270:
                    case ImagePropertyReader.Orientation.D90:
                        int tmp = width;
                        width = height;
                        height = tmp;
                        break;
                }
            }

            return (width, height);
        }

        public void Resize(Stream inputContent, Stream outputContent, int desiredWidth)
        {
            using (var input = IsImage.Load(inputContent))
            {
                EnsureExifImageRotation(input);

                var (width, height) = GetResizedBounds(input.Width, input.Height, desiredWidth);
                if (width != input.Width)
                    Resize(input, outputContent, null, width, height);
                else
                    SaveImage(outputContent, input);
            }
        }

        private void SaveImage(Stream outputContent, IsImage target)
            => target.Save(outputContent, formatDefinition.Codec);

        private void Resize(IsImage input, Stream outputContent, Rectangle? inputRect, int width, int height)
        {
            using var target = input.Clone(x =>
            {
                if (inputRect != null)
                    x.Crop(inputRect.Value);

                x.Resize(width, height);
            });
            SaveImage(outputContent, target);
        }

        private void EnsureExifImageRotation(IsImage image)
            => image.Mutate(x => x.AutoOrient());
    }
}
