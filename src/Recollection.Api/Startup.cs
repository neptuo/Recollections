using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Recollection.Accounts;

namespace Neptuo.Recollection
{
    public class Startup
    {
        private readonly IConfiguration configuration;
        private readonly AccountsStartup accountsStartup;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.accountsStartup = new AccountsStartup(configuration);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            accountsStartup.ConfigureServices(services);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseStatusCodePages();

            UseCors(app);

            accountsStartup.ConfigureAuthentication(app, env);

            app.UseMvc();
        }

        private static void UseCors(IApplicationBuilder app)
        {
            app.UseCors(p =>
            {
                p.WithOrigins("http://localhost:5000", "http://localhost:33885");
                p.AllowAnyMethod();
                p.AllowCredentials();
                p.AllowAnyHeader();
                p.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        }
    }
}
