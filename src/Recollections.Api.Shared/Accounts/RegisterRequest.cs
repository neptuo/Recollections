using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class RegisterRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public RegisterRequest()
        { }

        public RegisterRequest(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }
    }
}
