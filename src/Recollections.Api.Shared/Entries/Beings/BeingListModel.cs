using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Beings
{
    public class BeingListModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public int Entries { get; set; }

    }
}
