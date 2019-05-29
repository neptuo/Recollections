using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries
{
    public class EntriesStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<Api>();
        }
    }
}
