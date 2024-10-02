using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class VersionInfo
    {
        [Inject]
        protected Api Api { get; set; }

        [Parameter]
        public bool Separator { get; set; } = true;

        protected VersionModel ApiVersion { get; private set; }
        protected VersionModel ClientVersion { get; private set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            ClientVersion = new VersionModel(typeof(VersionInfo).Assembly.GetName().Version);
            ApiVersion = await GetApiVersionAsync(Api);
        }

        private static Task<VersionModel> getApiVersionTask;
        private static Task<VersionModel> GetApiVersionAsync(Api api)
        {
            if (getApiVersionTask == null)
                getApiVersionTask = api.GetVersionAsync();

            return getApiVersionTask;
        }
    }
}
