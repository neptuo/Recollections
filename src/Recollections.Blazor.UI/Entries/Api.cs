using Microsoft.AspNetCore.Components;
using Neptuo;
using Neptuo.Activators;
using Neptuo.Exceptions.Handlers;
using Neptuo.Logging;
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
        private readonly TaskFaultHandler faultHandler;

        public Api(IFactory<HttpClient> httpFactory, UrlResolver urlResolver, TaskFaultHandler faultHandler)
        {
            Ensure.NotNull(httpFactory, "httpFactory");
            Ensure.NotNull(urlResolver, "urlResolver");
            Ensure.NotNull(faultHandler, "faultHandler");
            this.http = httpFactory.Create();
            this.urlResolver = urlResolver;
            this.faultHandler = faultHandler;
        }

        public Task<TimelineListResponse> GetTimelineListAsync(int? offset)
            => faultHandler.Wrap(http.GetJsonAsync<TimelineListResponse>(urlResolver($"/timeline/list{(offset != null && offset > 0 ? $"?offset={offset}" : null)}")));

        public Task<List<MapEntryModel>> GetMapListAsync()
            => faultHandler.Wrap(http.GetJsonAsync<List<MapEntryModel>>(urlResolver("/map/list")));

        public Task<EntryModel> CreateAsync(EntryModel model)
            => faultHandler.Wrap(http.PostJsonAsync<EntryModel>(urlResolver("/entries"), model));

        public Task UpdateAsync(EntryModel model)
            => faultHandler.Wrap(http.PutJsonAsync(urlResolver($"/entries/{model.Id}"), model));

        public Task DeleteAsync(string entryId)
            => faultHandler.Wrap(http.DeleteAsync(urlResolver($"/entries/{entryId}")));

        public Task<EntryModel> GetDetailAsync(string entryId)
            => faultHandler.Wrap(http.GetJsonAsync<EntryModel>(urlResolver($"/entries/{entryId}")));

        public Task<List<ImageModel>> GetImagesAsync(string entryId)
            => faultHandler.Wrap(http.GetJsonAsync<List<ImageModel>>(urlResolver($"/entries/{entryId}/images")));

        public Task<ImageModel> GetImageAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.GetJsonAsync<ImageModel>(urlResolver($"/entries/{entryId}/images/{imageId}")));

        public Task UpdateImageAsync(string entryId, ImageModel model)
            => faultHandler.Wrap(http.PutJsonAsync(urlResolver($"/entries/{entryId}/images/{model.Id}"), model));

        public Task DeleteImageAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.DeleteAsync(urlResolver($"/entries/{entryId}/images/{imageId}")));

        public Task SetImageLocationFromOriginalAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.PostAsync(urlResolver($"/entries/{entryId}/images/{imageId}/set-location-from-original"), new StringContent(String.Empty)));

        public string ImageUploadUrl(string entryId) => urlResolver($"/entries/{entryId}/images");

        public string ResolveUrl(string relativeUrl) => urlResolver(relativeUrl).Replace("apiapi", "api");

        public Task<VersionModel> GetVersionAsync()
            => faultHandler.Wrap(http.GetJsonAsync<VersionModel>(urlResolver($"/entries/version")));
    }
}
