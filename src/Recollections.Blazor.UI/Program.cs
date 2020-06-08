using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Events;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Commons;
using Neptuo.Recollections.Entries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class Program
    {
        private static CommonStartup common;
        private static AccountsStartup accounts;
        private static EntriesStartup entries;

        public async static Task Main(string[] args)
        {
            // Startups
            common = new CommonStartup();
            accounts = new AccountsStartup();
            entries = new EntriesStartup();

            // Configure.
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault();
            ConfigureServices(builder.Services);
            ConfigureComponents(builder.RootComponents);

            // Startup.
            WebAssemblyHost host = builder.Build();
            StartupServices(host.Services);

            // Run.
            await host.RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services)
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

        private static void ConfigureComponents(RootComponentMappingCollection rootComponents)
        {
            rootComponents.Add<App>("app");
        }

        private static void StartupServices(IServiceProvider services)
        {
        }

        private static string Resolve(string appRelative)
        {
#if DEBUG
            return $"http://localhost:33880/api{appRelative}";
#else
            return $"https://api.recollections.neptuo.com/api{appRelative}";
#endif
        }
    }
}
