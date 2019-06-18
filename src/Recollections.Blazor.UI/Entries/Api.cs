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

        public Task<EntryModel> CreateAsync(EntryModel model)
            => http.PostJsonAsync<EntryModel>(urlResolver("/entries"), model);

        public Task UpdateAsync(EntryModel model)
            => http.PutJsonAsync(urlResolver($"/entries/{model.Id}"), model);

        public Task DeleteAsync(string entryId)
            => http.DeleteAsync(urlResolver($"/entries/{entryId}"));

        public Task<EntryModel> GetDetailAsync(string entryId)
            => http.GetJsonAsync<EntryModel>(urlResolver($"/entries/{entryId}"));

        public string ImageUploadUrl(string entryId) => urlResolver($"/entries/{entryId}/images");
    }
}
