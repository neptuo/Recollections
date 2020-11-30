using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Http
{
    public class ApiStatusCodeMessageHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    throw new UnauthorizedAccessException();
                case HttpStatusCode.PaymentRequired:
                    throw new FreeLimitsReachedExceptionException();
                case HttpStatusCode.ServiceUnavailable:
                    throw new ServiceUnavailableException();
                default:
                    return response;
            }
        }
    }
}
