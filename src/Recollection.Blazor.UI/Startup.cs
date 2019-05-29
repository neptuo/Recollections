using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Activators;
using Neptuo.Recollection.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            services.AddSingleton<HttpClient>();
            services.AddSingleton<UrlResolver>(Resolve);

            accounts.ConfigureServices(services);
        }

        private static string Resolve(string appRelative) => $"http://localhost:33880/api{appRelative}";

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
