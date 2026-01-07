using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neptuo.Events;
using Neptuo.Events.Handlers;
using Neptuo.Recollections.Accounts.Events;
using Neptuo.Recollections.Entries.Events.Handlers;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Transforms;

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

        public void ConfigureServices(IServiceCollection services, IHostEnvironment environment, IReverseProxyBuilder yarp)
        {
            ConfigureMedia(services);
            ConfigureDatabase(services);
            ConfigureStorage(services);
            ConfigureFreeLimits(services);

            services
                .AddHealthChecks()
                .AddDbContextCheck<DataContext>("Entries.DataContext");

            services
                .AddTransient<UserBeingService>()
                .AddTransient<IEventHandler<UserRegistered>, UserHandler>();

            services
                .Configure<MapOptions>(configuration.GetSection("Map"));

            services
                .AddHttpClient("mapy.cz", (services, http) => 
                {
                    string apiKey = services.GetService<IOptions<MapOptions>>().Value.ApiKey;
                    if (!string.IsNullOrEmpty(apiKey))
                        http.DefaultRequestHeaders.Add("X-Mapy-Api-Key", apiKey);
                    
                    http.BaseAddress = new Uri("https://api.mapy.cz/", UriKind.Absolute);
                });

            yarp
                .AddTransforms(builderContext =>
                {
                    if (builderContext.Route.ClusterId == "maptiles")
                    {
                        string apiKey = builderContext.Services.GetService<IOptions<MapOptions>>().Value.ApiKey;
                        if (!string.IsNullOrEmpty(apiKey))
                            builderContext.AddRequestHeader("X-Mapy-Api-Key", apiKey);
                    }
                });

            if (environment.IsDevelopment())
                EnsureDatabase(services);
        }

        private void ConfigureFreeLimits(IServiceCollection services)
        {
            services.Configure<FreeLimitsOptions>(configuration.GetSection("FreeLimits"));
            services.AddTransient<FreeLimitsChecker>();
        }

        private static void ConfigureMedia(IServiceCollection services)
        {
            services
                .AddTransient<ImageService>()
                .AddTransient<VideoService>()
                .AddTransient<ImageResizeService>()
                .AddTransient<TimelineService>()
                .AddTransient<IImageValidator, PremiumImageSizeValidator>()
                .AddTransient<IVideoValidator, VideoValidator>()
                .AddSingleton(ImageFormatDefinition.Jpeg);
        }

        private void ConfigureDatabase(IServiceCollection services)
        {
            services.AddDbContextWithSchema<DataContext>(configuration.GetSection("Database"), pathResolver);
        }

        private void ConfigureStorage(IServiceCollection services)
        {
            services
                .Configure<StorageOptions>(configuration.GetSection("Storage"));

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

        public void Configure(IServiceProvider services)
        {
            var events = services.GetRequiredService<IEventHandlerCollection>();
            events.Add(new ServiceProviderEventHandler<UserRegistered>(services));
        }
    }
}
