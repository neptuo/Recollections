using Neptuo.Activators;
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
    }
}
