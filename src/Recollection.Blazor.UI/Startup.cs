using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Recollection.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollection
{
    public class Startup
    {
        private readonly AccountsStartup accounts;

        public Startup()
        {
            accounts = new AccountsStartup();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            accounts.ConfigureServices(services);
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
