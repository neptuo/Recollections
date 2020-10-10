using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing
{
    public class ShareModel
    {
        /// <summary>
        /// Gets a shared-with username or null for publicly visible items.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets a share permission
        /// </summary>
        public Permission Permission { get; set; }

        public ShareModel()
        { }

        public ShareModel(string userName, Permission permission)
        {
            Ensure.NotNull(permission, "permission");
            UserName = userName;
            Permission = permission;
        }
    }
}
