using Microsoft.AspNetCore.Components;
using Neptuo.Activators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
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

        public Task<List<ImageModel>> GetImagesAsync(string entryId)
            => http.GetJsonAsync<List<ImageModel>>(urlResolver($"/entries/{entryId}/images"));

        public Task<ImageModel> GetImageAsync(string entryId, string imageId)
            => http.GetJsonAsync<ImageModel>(urlResolver($"/entries/{entryId}/images/{imageId}"));

        public Task UpdateImageAsync(string entryId, ImageModel model)
            => http.PutJsonAsync(urlResolver($"/entries/{entryId}/images/{model.Id}"), model);

        public Task DeleteImageAsync(string entryId, string imageId)
            => http.DeleteAsync(urlResolver($"/entries/{entryId}/images/{imageId}"));

        public Task SetImageLocationFromOriginalAsync(string entryId, string imageId)
            => http.PostAsync(urlResolver($"/entries/{entryId}/images/{imageId}/set-location-from-original"), new StringContent(String.Empty));

        public string ImageUploadUrl(string entryId) => urlResolver($"/entries/{entryId}/images");

        public string ResolveUrl(string relativeUrl) => urlResolver(relativeUrl).Replace("apiapi", "api");

        public Task<VersionModel> GetVersionAsync()
            => http.GetJsonAsync<VersionModel>(urlResolver($"/entries/version"));
    }
}
