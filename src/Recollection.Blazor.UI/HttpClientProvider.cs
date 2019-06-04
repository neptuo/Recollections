using Neptuo;
using Neptuo.Activators;
using Neptuo.Recollection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection
{
    public class HttpClientProvider : IFactory<HttpClient>
    {
        private readonly HttpClient http;

        public HttpClientProvider(HttpClient http)
        {
            Ensure.NotNull(http, "http");
            this.http = http;

            Console.WriteLine("HttpClientProvider");
        }

        public HttpClient Create() => http;
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientFactoryServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpClientFactory(this IServiceCollection services)
        {
            Ensure.NotNull(services, "services");
            return services.AddSingleton<IFactory<HttpClient>, HttpClientProvider>();
        }
    }
}
