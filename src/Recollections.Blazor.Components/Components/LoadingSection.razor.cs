using Microsoft.AspNetCore.Components;

namespace Neptuo.Recollections.Components
{
    public partial class LoadingSection : ComponentBase
    {
        [Parameter]
        public bool IsLoading { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
