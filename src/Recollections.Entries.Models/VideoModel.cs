using System;

namespace Neptuo.Recollections.Entries
{
    public class VideoModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }

        public MediaSourceModel Thumbnail { get; set; }
        public MediaSourceModel Preview { get; set; }
        public MediaSourceModel Original { get; set; }

        public string ContentType { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime When { get; set; }
    }
}
