using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;

namespace Neptuo.Recollections
{
    public class Startup
    {
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment environment;
        private readonly AccountsStartup accountsStartup;
        private readonly EntriesStartup entriesStartup;
        private readonly SharingStartup sharingStartup;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(environment, "environment");
            this.configuration = configuration;
            this.environment = environment;

            accountsStartup = new AccountsStartup(configuration.GetSection("Accounts"), ResolvePath);
            entriesStartup = new EntriesStartup(configuration.GetSection("Entries"), ResolvePath);
            sharingStartup = new SharingStartup();
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

            services.Configure<CorsOptions>(configuration.GetSection("Cors"));

            accountsStartup.ConfigureServices(services);
            entriesStartup.ConfigureServices(services);
            sharingStartup.ConfigureServices(services);
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
            var options = app.ApplicationServices.GetRequiredService<IOptions<CorsOptions>>().Value;

            app.UseCors(p =>
            {
                p.WithOrigins(options.Origins);
                p.AllowAnyMethod();
                p.AllowCredentials();
                p.AllowAnyHeader();
                p.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        }
    }
}
