using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class AccountsStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<Api>();

            services
                .AddTransient<TokenStorage>()
                .AddSingleton<PropertyCollection>()
                .AddBlazoredLocalStorage()
                .AddBlazoredSessionStorage();
        }
    }
}
