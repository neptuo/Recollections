using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Components.Editors
{
    public class InlineDateEditModel : InlineEditModel<DateTime>
    {
        [Parameter]
        protected string DisplayFormat { get; set; }
    }
}
