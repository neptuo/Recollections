using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public interface IFileInput
    {
        string ContentType { get; }
        string FileName { get; }
        long Length { get; }

        Stream OpenReadStream();
    }
}
