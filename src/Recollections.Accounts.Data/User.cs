using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class User : IdentityUser
    {
        public DateTime Created { get; set; }

        public ICollection<UserPropertyValue> Properties { get; set; }

        public User()
        {
            Created = DateTime.Now;
        }

        public User(string userName)
            : base(userName)
        {
            Created = DateTime.Now;
        }
    }
}
