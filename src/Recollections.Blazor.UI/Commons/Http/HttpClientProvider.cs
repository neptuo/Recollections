using Neptuo;
using Neptuo.Activators;
using Neptuo.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class HttpClientProvider : IFactory<HttpClient>
    {
        private readonly HttpClient http;

        public HttpClientProvider(IHttpClientFactory httpFactory, ILog<HttpClientProvider> log)
        {
            Ensure.NotNull(httpFactory, "httpFactory");
            http = httpFactory.CreateClient("Api");

            log.Debug("HttpClientProvider.ctor");
        }

        public HttpClient Create() => http;
    }
}

