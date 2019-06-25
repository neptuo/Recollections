using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class Image
    {
        [Key]
        public string Id { get; set; }

        public Entry Entry { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public DateTime When { get; set; }

        public string FileName { get; set; }
    }
}
