using Microsoft.AspNetCore.Components;

namespace Neptuo.Recollections.Components
{
    public partial class Loading : ComponentBase
    {
        [Parameter]
        public bool IsLoading { get; set; }
    }
}
