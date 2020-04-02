using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages
{
    public partial class Profile
    {
        [Parameter]
        public string UserName { get; set; }
    }
}
