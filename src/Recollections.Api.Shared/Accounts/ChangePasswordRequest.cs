using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class ChangePasswordRequest
    {
        public string Current { get; set; }
        public string New { get; set; }

        public ChangePasswordRequest()
        { }

        public ChangePasswordRequest(string current, string newPassword)
        {
            Current = current;
            New = newPassword;
        }
    }
}
