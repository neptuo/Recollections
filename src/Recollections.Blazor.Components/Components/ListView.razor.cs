using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public partial class ListView<T> : ComponentBase
    {
        [Inject]
        protected ILog<ListView<T>> Log { get; set; }

        [Parameter]
        public bool IsLoading { get; set; }

        [Parameter]
        public IReadOnlyCollection<T> Items { get; set; }

        [Parameter]
        public string EmptyMessage { get; set; } = "No data...";

        [Parameter]
        public RenderFragment EmptyContent { get; set; }

        [Parameter]
        public RenderFragment<T> ChildContent { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            Log.Debug($"Count: {Items?.Count}");
        }
    }
}
