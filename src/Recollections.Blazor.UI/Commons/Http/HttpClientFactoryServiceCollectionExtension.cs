using Neptuo;
using Neptuo.Activators;
using Neptuo.Recollections;
using Neptuo.Recollections.Commons.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientFactoryServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpClientFactory(this IServiceCollection services, string baseUrl)
        {
            Ensure.NotNull(services, "services");

            services
                .AddTransient<ApiStatusCodeMessageHandler>();

            services
                .AddHttpClient("Api")
                .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl, UriKind.Absolute))
                .AddHttpMessageHandler(sp => sp.GetRequiredService<ApiStatusCodeMessageHandler>());

            return services
                .Configure<ApiSettings>(s => s.BaseUrl = baseUrl)
                .AddSingleton<IFactory<HttpClient>, HttpClientProvider>();
        }
    }
}
