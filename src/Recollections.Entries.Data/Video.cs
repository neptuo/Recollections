using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neptuo.Recollections.Entries
{
    public class Video
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public Entry Entry { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime When { get; set; }
        public DateTime Created { get; set; }

        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
        public double? Duration { get; set; }

        public ImageLocation Location { get; set; } = new();

        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}
