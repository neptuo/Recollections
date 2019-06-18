﻿using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class IconModel : ComponentBase
    {
        [Parameter]
        protected string Identifier { get; set; }

        [Parameter]
        protected Action OnClick { get; set; }
    }
}
