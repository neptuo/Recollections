using SimpleJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class Json
    {
        public T Deserialize<T>(string json) 
            => SimpleJson.SimpleJson.DeserializeObject<T>(json, CamelCaseSerializerStrategy.Instance);

        public string Serialize(object instance)
            => SimpleJson.SimpleJson.SerializeObject(instance, CamelCaseSerializerStrategy.Instance);
    }
}
