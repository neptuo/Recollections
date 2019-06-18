using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class EntriesStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<Api>();
        }
    }
}
