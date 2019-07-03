using System;
using System.Collections.Generic;
using System.Drawing;
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
        public void Thumbnail(string inputPath, string outputPath, int width, int height)
        {
            using (var image = DrImage.FromFile(inputPath))
            {
                int resizeWidth = width;
                int resizeHeight = height;

                if (image.Width < image.Height)
                {
                    var resizeRatio = resizeWidth / (double)image.Width;
                    resizeHeight = (int)(resizeRatio * image.Height);
                }
                else
                {
                    var resizeRatio = resizeHeight / (double)image.Height;
                    resizeWidth = (int)(resizeRatio * image.Width);
                }

                using (Bitmap thumbnail = (Bitmap)image.GetThumbnailImage(resizeWidth, resizeHeight, GetThumbnalImageAbort, IntPtr.Zero))
                {
                    if (width != resizeWidth || height != resizeHeight)
                    {
                        int x = (resizeWidth - width) / 2;
                        int y = (resizeHeight - height) / 2;

                        using (var cropped = thumbnail.Clone(new Rectangle(x, y, width, height), PixelFormat.DontCare))
                            cropped.Save(outputPath);
                    }
                    else
                    {
                        thumbnail.Save(outputPath);
                    }
                }
            }
        }

        public void Resize(string inputPath, string outputPath, int width)
        {
            using (var input = DrImage.FromFile(inputPath))
            {
                if (width < input.Width)
                {
                    var ratio = width / (double)input.Width;
                    int height = (int)(ratio * input.Height);

                    using (var target = input.GetThumbnailImage(width, height, GetThumbnalImageAbort, IntPtr.Zero))
                        target.Save(outputPath);
                }
                else
                {
                    input.Save(outputPath);
                }
            }
        }

        private bool GetThumbnalImageAbort() => false;
    }
}
