using Neptuo;
using Neptuo.Activators;
using Neptuo.Logging;
using Neptuo.Recollections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class HttpClientProvider : IFactory<HttpClient>
    {
        private readonly HttpClient http;

        public HttpClientProvider(HttpClient http, ILog<HttpClientProvider> log)
        {
            Ensure.NotNull(http, "http");
            this.http = http;

            log.Debug("HttpClientProvider.ctor");
        }

        public HttpClient Create() => http;
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HttpClientFactoryServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpClientFactory(this IServiceCollection services, string baseUrl)
        {
            Ensure.NotNull(services, "services");
            return services
                .Configure<ApiSettings>(s => s.BaseUrl = baseUrl)
                .AddSingleton(p => new HttpClient() { BaseAddress = new Uri(baseUrl, UriKind.Absolute) })
                .AddSingleton<IFactory<HttpClient>, HttpClientProvider>();
        }
    }
}
