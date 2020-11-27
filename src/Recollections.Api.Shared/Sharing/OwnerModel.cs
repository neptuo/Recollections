using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing
{
    public class OwnerModel
    {
        public string Id { get; }
        public string Name { get; }

        public OwnerModel(string id, string name)
        {
            Ensure.NotNullOrEmpty(id, "id");
            Ensure.NotNullOrEmpty(name, "name");
            Id = id;
            Name = name;
        }
    }
}
