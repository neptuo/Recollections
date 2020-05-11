using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public interface IFileStorage
    {
        Task<Stream> FindAsync(Entry entry, Image image, ImageType type);
        Task SaveAsync(Entry entry, Image image, Stream content, ImageType type);
        Task DeleteAsync(Entry entry, Image image, ImageType type);
    }
}
