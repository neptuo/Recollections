using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public interface IUserNameProvider
    {
        Task<IReadOnlyList<string>> GetUserIdsAsync(IReadOnlyCollection<string> userNames);
        Task<IReadOnlyList<string>> GetUserNamesAsync(IReadOnlyCollection<string> userIds);
    }
}
