using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Accounts
{
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public LoginRequest()
        { }

        public LoginRequest(string username, string password)
        {
            Ensure.NotNull(username, "username");
            Ensure.NotNull(password, "password");
            Username = username;
            Password = password;
        }
    }
}
