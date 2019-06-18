using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Accounts
{
    public class RegisterResponse
    {
        public bool IsSuccess => ErrorMessages.Count == 0;

        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}
