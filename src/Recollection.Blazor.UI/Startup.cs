using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Activators;
using Neptuo.Recollection.Accounts;
using Neptuo.Recollection.Components;
using Neptuo.Recollection.Entries;
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
        private readonly EntriesStartup entries;

        public Startup()
        {
            accounts = new AccountsStartup();
            entries = new EntriesStartup();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddUiOptions()
                .AddSingleton<IFactory<HttpClient>, HttpClientProvider>()
                .AddSingleton<UrlResolver>(Resolve)
                .AddTransient<Navigator>()
                .AddSingleton<ModalNative>();

            accounts.ConfigureServices(services);
            entries.ConfigureServices(services);
        }

        private static string Resolve(string appRelative) => $"http://localhost:33880/api{appRelative}";

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
