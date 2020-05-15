using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;

namespace Neptuo.Recollections
{
    public class Startup
    {
        private readonly IWebHostEnvironment environment;
        private readonly AccountsStartup accountsStartup;
        private readonly EntriesStartup entriesStartup;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Ensure.NotNull(environment, "environment");
            this.environment = environment;

            accountsStartup = new AccountsStartup(configuration.GetSection("Accounts"), ResolvePath);
            entriesStartup = new EntriesStartup(configuration.GetSection("Entries"), ResolvePath);
        }

        private string ResolvePath(string relativePath)
        {
            Ensure.NotNull(relativePath, "relativePath");
            return relativePath.Replace("{BasePath}", environment.ContentRootPath);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<PathResolver>(ResolvePath);

            services
                .AddRouting(options => options.LowercaseUrls = true)
                .AddControllers()
                .AddNewtonsoftJson();

            accountsStartup.ConfigureServices(services);
            entriesStartup.ConfigureServices(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            if (environment.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseStatusCodePages();

            app.UseRouting();

            UseCors(app);

            accountsStartup.ConfigureAuthentication(app, environment);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllers();
            });
        }

        private static void UseCors(IApplicationBuilder app)
        {
            app.UseCors(p =>
            {
#if DEBUG
                p.WithOrigins("http://localhost:33881");
#else
                p.WithOrigins("http://localhost:33881", "https://app.recollections.neptuo.com");
#endif
                p.AllowAnyMethod();
                p.AllowCredentials();
                p.AllowAnyHeader();
                p.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        }
    }
}
