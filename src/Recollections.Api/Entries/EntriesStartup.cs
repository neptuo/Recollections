using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Neptuo.Recollections.Entries.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class EntriesStartup
    {
        private readonly IConfiguration configuration;
        private readonly PathResolver pathResolver;

        public EntriesStartup(IConfiguration configuration, PathResolver pathResolver)
        {
            this.configuration = configuration;
            this.pathResolver = pathResolver;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTransient<ImageService>()
                .AddTransient<ImageResizeService>()
                .AddDbContext<DataContext>(options => options.UseSqlite(pathResolver(configuration.GetValue<string>("ConnectionString"))))
                .Configure<StorageOptions>(configuration.GetSection("Storage"));

            EnsureDatabase(services);
        }

        private static void EnsureDatabase(IServiceCollection services)
        {
            try
            {
                using (var scope = services.BuildServiceProvider().CreateScope())
                {
                    var provider = scope.ServiceProvider;
                    var db = provider.GetService<DataContext>();

                    db.Database.EnsureCreated();
                    db.Database.Migrate();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
