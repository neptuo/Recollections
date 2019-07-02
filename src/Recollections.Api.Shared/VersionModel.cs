using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class VersionModel
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }

        public VersionModel()
        { }

        public VersionModel(Version model)
        {
            Ensure.NotNull(model, "model");
            Major = model.Major;
            Minor = model.Minor;
            Patch = model.Build;
        }
    }
}
