using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing
{
    public class SharingStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTransient<ShareStatusService>()
                .AddTransient<ShareDeleter>();
        }
    }
}
