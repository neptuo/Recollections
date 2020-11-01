using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public interface IUserPremiumProvider
    {
        public Task<bool> HasPremiumAsync(string userId);
    }
}
