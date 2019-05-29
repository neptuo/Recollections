using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neptuo;
using Neptuo.Recollection.Accounts;
using Neptuo.Recollection.Entries;

namespace Neptuo.Recollection
{
    public delegate string PathResolver(string relativePath);

    public class Startup
    {
        private readonly IHostingEnvironment environment;
        private readonly AccountsStartup accountsStartup;
        private readonly EntriesStartup entriesStartup;

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Ensure.NotNull(environment, "environment");
            this.environment = environment;

            accountsStartup = new AccountsStartup(configuration.GetSection("Accounts"), ResolvePath);
            entriesStartup = new EntriesStartup(configuration.GetSection("Entries"), ResolvePath);
        }

        private string ResolvePath(string relativePath) => relativePath.Replace("{BasePath}", environment.ContentRootPath);

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            accountsStartup.ConfigureServices(services);
            entriesStartup.ConfigureServices(services);
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
