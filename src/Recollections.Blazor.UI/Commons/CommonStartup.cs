using Microsoft.Extensions.DependencyInjection;
using Neptuo.Recollections.Commons.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons
{
    public class CommonStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<PwaInstallInterop>();
        }
    }
}
