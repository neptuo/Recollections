﻿using Microsoft.AspNetCore.Components;
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

        protected VersionModel ApiVersion { get; private set; }
        protected VersionModel ClientVersion { get; private set; }

        protected async override Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            ClientVersion = new VersionModel(typeof(VersionInfo).Assembly.GetName().Version);
            ApiVersion = await Api.GetVersionAsync();
        }
    }
}
