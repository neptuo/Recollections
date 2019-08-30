using SimpleJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleJson
{
    class CamelCaseSerializerStrategy : PocoJsonSerializerStrategy
    {
        protected override string MapClrMemberNameToJsonFieldName(string clrPropertyName) 
            => clrPropertyName.Substring(0, 1).ToLower() + clrPropertyName.Substring(1);

        public readonly static CamelCaseSerializerStrategy Instance = new CamelCaseSerializerStrategy();
    }
}
