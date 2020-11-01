using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public interface IImageValidator
    {
        Task ValidateAsync(string userId, IFileInput file);
    }
}
