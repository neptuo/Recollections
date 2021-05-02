using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class TokenLoginOptions
    {
        public Dictionary<string, string> Tokens { get; } = new Dictionary<string, string>();
    }
}
