using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class VersionInfoModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        protected VersionModel ApiVersion { get; private set; }
        protected VersionModel ClientVersion { get; private set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            ClientVersion = new VersionModel(typeof(VersionInfoModel).Assembly.GetName().Version);
            ApiVersion = await Api.GetVersionAsync();
        }
    }
}
