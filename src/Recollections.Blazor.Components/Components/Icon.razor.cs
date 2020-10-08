using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    partial class Icon : ComponentBase
    {
        [Parameter]
        public string Identifier { get; set; }

        [Parameter]
        public string Prefix { get; set; } = "fa";

        [Parameter]
        public Action OnClick { get; set; }
    }
}
