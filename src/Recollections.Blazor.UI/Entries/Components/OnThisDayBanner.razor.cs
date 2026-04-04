using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class OnThisDayBanner
    {
        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        protected int Count { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Count = await Api.GetOnThisDayCountAsync();
        }
    }
}
