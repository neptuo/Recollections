using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Recollections.Entries.Services;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
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
                .AddSingleton(new ImageFormatDefinition(ImageFormat.Jpeg, ".jpg"))
                .AddDbContext<DataContext>(options => options.UseSqlite(pathResolver(configuration.GetValue<string>("ConnectionString"))))
                .Configure<StorageOptions>(configuration.GetSection("Storage"));

            services
                .AddHealthChecks()
                .AddDbContextCheck<DataContext>("Entries.DataContext");

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
