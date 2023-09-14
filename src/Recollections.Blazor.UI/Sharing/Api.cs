using Neptuo.Activators;
using Neptuo.Recollections.Commons.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        private async Task<bool> SaveAsync(string url, List<ShareModel> model) 
        {
            var response = await http.PutAsJsonAsync(url, model);
            if (response.IsSuccessStatusCode)
                return true;
            else if (response.StatusCode == HttpStatusCode.BadRequest)
                return false;
            else
                response.EnsureSuccessStatusCode();

            return false;
        }

        public Task<List<ShareModel>> GetEntryListAsync(string entryId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ShareModel>>($"entries/{entryId}/sharing"));

        public Task<bool> SaveEntryAsync(string entryId, List<ShareModel> model)
            => faultHandler.Wrap(SaveAsync($"entries/{entryId}/sharing", model));

        public Task<List<ShareModel>> GetStoryListAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ShareModel>>($"stories/{storyId}/sharing"));

        public Task<bool> SaveStoryAsync(string storyId, List<ShareModel> model)
            => faultHandler.Wrap(SaveAsync($"stories/{storyId}/sharing", model));

        public Task<List<ShareModel>> GetBeingListAsync(string beingId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ShareModel>>($"beings/{beingId}/sharing"));

        public Task<bool> SaveBeingAsync(string beingId, List<ShareModel> model)
            => faultHandler.Wrap(SaveAsync($"beings/{beingId}/sharing", model));
    }
}
