using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neptuo.Recollections.Accounts;
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

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment)
        {
            ConfigureImages(services);
            ConfigureDatabase(services);
            ConfigureStorage(services);
            ConfigureFreeLimits(services);

            services
                .AddHealthChecks()
                .AddDbContextCheck<DataContext>("Entries.DataContext");

            if (environment.IsDevelopment())
                EnsureDatabase(services);
        }

        private void ConfigureFreeLimits(IServiceCollection services)
        {
            services.Configure<FreeLimitsOptions>(configuration.GetSection("FreeLimits"));
            services.AddTransient<FreeLimitsChecker>();
        }

        private static void ConfigureImages(IServiceCollection services)
        {
            services
                .AddTransient<ImageService>()
                .AddTransient<ImageResizeService>()
                .AddTransient<IImageValidator, PermiumImageSizeValidator>()
                .AddSingleton(ImageFormatDefinition.Jpeg);
        }

        private void ConfigureDatabase(IServiceCollection services)
        {
            services.AddDbContextWithSchema<DataContext>(configuration.GetSection("Database"), pathResolver);
        }

        private void ConfigureStorage(IServiceCollection services)
        {
            services.Configure<StorageOptions>(configuration.GetSection("Storage"));

            var fileSystem = configuration.GetSection("Storage").GetSection("FileSystem");
            if (fileSystem.GetValue("Server", StorageFileSystem.Local) == StorageFileSystem.Azure)
            {
                services
                    .AddTransient<IFileStorage, AzureFileStorage>()
                    .Configure<AzureStorageOptions>(fileSystem);
            }
            else
            {
                services
                    .AddTransient<IFileStorage, SystemIoFileStorage>()
                    .Configure<SystemIoStorageOptions>(fileSystem);
            }
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
