using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Activators;
using Neptuo.Events;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Commons;
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
        private readonly CommonStartup common;
        private readonly AccountsStartup accounts;
        private readonly EntriesStartup entries;

        public Startup()
        {
            common = new CommonStartup();
            accounts = new AccountsStartup();
            entries = new EntriesStartup();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            DefaultEventManager eventManager = new DefaultEventManager();

            services
                .AddLogging()
                .AddExceptions()
                .AddComponents()
                .AddUiOptions()
                .AddHttpClientFactory()
                .AddSingleton<IEventDispatcher>(eventManager)
                .AddSingleton<IEventHandlerCollection>(eventManager)
                .AddSingleton<Json>()
                .AddSingleton<UrlResolver>(Resolve)
                .AddTransient<Navigator>();

            common.ConfigureServices(services);
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
