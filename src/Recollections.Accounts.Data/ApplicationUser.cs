using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Accounts
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime Created { get; set; }

        public ApplicationUser()
        {
            Created = DateTime.Now;
        }

        public ApplicationUser(string userName)
            : base(userName)
        {
            Created = DateTime.Now;
        }
    }
}
