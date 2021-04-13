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
        public string Prefix { get; set; }

        [Parameter]
        public string CssClass { get; set; }

        [Parameter]
        public Action OnClick { get; set; }

        protected string FullCssClass { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (String.IsNullOrEmpty(Prefix))
                Prefix = "fa";

            FullCssClass = String.Join(" ", Prefix, "fa-" + Identifier, CssClass);
        }
    }
}
