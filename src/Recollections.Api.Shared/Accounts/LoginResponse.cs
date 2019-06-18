using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class LoginResponse
    {
        public string BearerToken { get; set; }

        public LoginResponse()
        { }

        public LoginResponse(string bearerToken)
        {
            Ensure.NotNull(bearerToken, "bearerToken");
            BearerToken = bearerToken;
        }
    }
}
