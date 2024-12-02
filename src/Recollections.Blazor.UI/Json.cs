using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class Json
    {
        public T Deserialize<T>(string json)
            => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web);

        public string Serialize(object instance)
            => JsonSerializer.Serialize(instance, JsonSerializerOptions.Web);
    }
}
