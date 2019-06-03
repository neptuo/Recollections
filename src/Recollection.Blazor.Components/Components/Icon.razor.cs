using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Components
{
    public class IconModel : ComponentBase
    {
        [Parameter]
        protected string Identifier { get; set; }
    }
}
