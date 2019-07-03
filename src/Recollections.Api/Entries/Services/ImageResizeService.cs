using System;
using System.Collections.Generic;
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
            using (var thumbnail = image.GetThumbnailImage(width, height, GetThumbnalImageAbort, IntPtr.Zero))
                thumbnail.Save(outputPath);
        }

        private bool GetThumbnalImageAbort() => false;
    }
}
