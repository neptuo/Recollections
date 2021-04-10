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

        public Task CreateEntryAsync(string entryId, ShareModel model)
            => faultHandler.Wrap(http.PostAsJsonAsync($"entries/{entryId}/sharing", model));

        public Task DeleteEntryAsync(string entryId, ShareModel model)
            => faultHandler.Wrap(http.DeleteAsync($"entries/{entryId}/sharing/{model.UserName}"));

        public Task<List<ShareModel>> GetStoryListAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ShareModel>>($"stories/{storyId}/sharing"));

        public Task CreateStoryAsync(string storyId, ShareModel model)
            => faultHandler.Wrap(http.PostAsJsonAsync($"stories/{storyId}/sharing", model));

        public Task DeleteStoryAsync(string storyId, ShareModel model)
            => faultHandler.Wrap(http.DeleteAsync($"stories/{storyId}/sharing/{model.UserName}"));

        public Task<List<ShareModel>> GetBeingListAsync(string beingId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ShareModel>>($"beings/{beingId}/sharing"));

        public Task CreateBeingAsync(string beingId, ShareModel model)
            => faultHandler.Wrap(http.PostAsJsonAsync($"beings/{beingId}/sharing", model));

        public Task DeleteBeingAsync(string beingId, ShareModel model)
            => faultHandler.Wrap(http.DeleteAsync($"beings/{beingId}/sharing/{model.UserName}"));
    }
}
