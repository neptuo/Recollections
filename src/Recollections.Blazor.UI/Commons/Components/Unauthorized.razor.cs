using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Components
{
    public partial class Unauthorized
    {
        [CascadingParameter]
        internal UserState UserState { get; set; }
    }
}
