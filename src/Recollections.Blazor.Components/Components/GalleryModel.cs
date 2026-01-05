using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class GalleryModel
    {
        public string Title { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>
        /// Optional discriminator, e.g. "image" or "video".
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Optional media content type (used for videos when loading via stream).
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Optional poster URL (used for videos).
        /// </summary>
        public string PosterUrl { get; set; }
    }
}
