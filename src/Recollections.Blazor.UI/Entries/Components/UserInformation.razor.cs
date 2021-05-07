using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class UserInformation
    {
        [Inject]
        protected Navigator Navigator { get; set; }

        [CascadingParameter]
        public UserState UserState { get; set; }

        [Parameter]
        public OwnerModel Owner { get; set; }
    }
}
