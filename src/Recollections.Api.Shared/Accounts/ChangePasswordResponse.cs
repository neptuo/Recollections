using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class ChangePasswordResponse
    {
        public bool IsSuccess => ErrorMessages.Count == 0;

        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}
