using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class BeingIcon
    {
        [Parameter]
        public string Identifier { get; set; }

        protected string CssClass => String.IsNullOrEmpty(Identifier) ? "value-placeholder" : String.Empty;
        protected string TargetIdentifier => String.IsNullOrEmpty(Identifier) ? "user" : Identifier;
    }
}
