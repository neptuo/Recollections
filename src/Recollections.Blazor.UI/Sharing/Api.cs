using Neptuo.Activators;
using Neptuo.Recollections.Commons.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing
{
    public class Api
    {
        private readonly HttpClient http;
        private readonly TaskFaultHandler faultHandler;

        public Api(IFactory<HttpClient> httpFactory, TaskFaultHandler faultHandler)
        {
            Ensure.NotNull(httpFactory, "httpFactory");
            Ensure.NotNull(faultHandler, "faultHandler");
            this.http = httpFactory.Create();
            this.faultHandler = faultHandler;
        }

        public Task<List<ShareModel>> GetEntryListAsync(string entryId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ShareModel>>($"entries/{entryId}/sharing"));

        public Task SaveEntryAsync(string entryId, List<ShareModel> model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"entries/{entryId}/sharing", model));

        public Task<List<ShareModel>> GetStoryListAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ShareModel>>($"stories/{storyId}/sharing"));

        public Task SaveStoryAsync(string storyId, List<ShareModel> model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"stories/{storyId}/sharing", model));

        public Task<List<ShareModel>> GetBeingListAsync(string beingId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ShareModel>>($"beings/{beingId}/sharing"));

        public Task SaveBeingAsync(string beingId, List<ShareModel> model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"beings/{beingId}/sharing", model));
    }
}
