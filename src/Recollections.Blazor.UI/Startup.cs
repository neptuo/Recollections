using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Activators;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class Startup
    {
        private readonly AccountsStartup accounts;
        private readonly EntriesStartup entries;

        public Startup()
        {
            accounts = new AccountsStartup();
            entries = new EntriesStartup();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging()
                .AddComponents()
                .AddUiOptions()
                .AddHttpClientFactory()
                .AddSingleton<Json>()
                .AddSingleton<UrlResolver>(Resolve)
                .AddTransient<Navigator>();

            accounts.ConfigureServices(services);
            entries.ConfigureServices(services);
        }

        private static string Resolve(string appRelative)
        {
#if DEBUG
            return $"http://localhost:33880/api{appRelative}";
#else
            return $"https://api.recollections.neptuo.com/api{appRelative}";
#endif
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
