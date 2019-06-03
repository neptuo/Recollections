using Microsoft.AspNetCore.Components;
using Neptuo.Activators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Entries
{
    public class Api
    {
        private readonly HttpClient http;
        private readonly UrlResolver urlResolver;

        public Api(IFactory<HttpClient> httpFactory, UrlResolver urlResolver)
        {
            Ensure.NotNull(httpFactory, "httpFactory");
            Ensure.NotNull(urlResolver, "urlResolver");
            this.http = httpFactory.Create();
            this.urlResolver = urlResolver;
        }

        public Task<TimelineListResponse> GetListAsync(int? offset)
            => http.GetJsonAsync<TimelineListResponse>(urlResolver($"/timeline/list{(offset != null && offset > 0 ? $"?offset={offset}" : null)}"));

        public Task CreateAsync(EntryModel request)
            => http.PostJsonAsync(urlResolver("/entries"), request);

        public Task DeleteAsync(string entryId)
            => http.DeleteAsync(urlResolver($"/entries/{entryId}"));

        public Task<EntryModel> GetDetailAsync(string entryId)
            => http.GetJsonAsync<EntryModel>(urlResolver($"/entries/{entryId}"));
    }
}
