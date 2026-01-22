using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class AzureStorageOptions
    {
        public string ConnectionString { get; set; }
        public string FileShareName { get; set; }
    }
}
