using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Activators;
using Neptuo.Exceptions.Handlers;
using Neptuo.Logging;
using Neptuo.Recollections.Commons.Exceptions;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class Api
    {
        private readonly HttpClient http;
        private readonly ApiSettings settings;
        private readonly TaskFaultHandler faultHandler;

        public Api(IFactory<HttpClient> httpFactory, TaskFaultHandler faultHandler, IOptions<ApiSettings> settings)
        {
            Ensure.NotNull(httpFactory, "httpFactory");
            Ensure.NotNull(settings, "settings");
            Ensure.NotNull(faultHandler, "faultHandler");
            this.http = httpFactory.Create();
            this.settings = settings.Value;
            this.faultHandler = faultHandler;
        }

        public Task<TimelineListResponse> GetTimelineListAsync(int? offset)
            => faultHandler.Wrap(http.GetFromJsonAsync<TimelineListResponse>($"timeline/list{(offset != null && offset > 0 ? $"?offset={offset}" : null)}"));

        public Task<List<MapEntryModel>> GetMapListAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<List<MapEntryModel>>("map/list"));

        public Task<EntryModel> CreateEntryAsync(EntryModel model)
            => faultHandler.Wrap(http.PostAsJsonAsync<EntryModel, EntryModel>("entries", model));

        public Task UpdateEntryAsync(EntryModel model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"entries/{model.Id}", model));

        public Task DeleteEntryAsync(string entryId)
            => faultHandler.Wrap(http.DeleteAsync($"entries/{entryId}"));

        public Task<AuthorizedModel<EntryModel>> GetEntryAsync(string entryId)
            => faultHandler.Wrap(http.GetFromJsonAsync<AuthorizedModel<EntryModel>>($"entries/{entryId}"));

        public Task<List<ImageModel>> GetImagesAsync(string entryId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ImageModel>>($"entries/{entryId}/images"));

        public Task<AuthorizedModel<ImageModel>> GetImageAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.GetFromJsonAsync<AuthorizedModel<ImageModel>>($"entries/{entryId}/images/{imageId}"));

        public Task<byte[]> GetImageDataAsync(string url)
            => faultHandler.Wrap(http.GetByteArrayAsync((settings.BaseUrl + url).Replace("api/api", "api")));

        public Task UpdateImageAsync(string entryId, ImageModel model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"entries/{entryId}/images/{model.Id}", model));

        public Task DeleteImageAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.DeleteAsync($"entries/{entryId}/images/{imageId}"));

        public Task SetImageLocationFromOriginalAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.PostAsync($"entries/{entryId}/images/{imageId}/set-location-from-original", new StringContent(String.Empty)));

        public string ImageUploadUrl(string entryId) => $"{settings.BaseUrl}entries/{entryId}/images";

        public Task<List<StoryListModel>> GetStoryListAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<List<StoryListModel>>("stories"));

        public Task<List<StoryChapterListModel>> GetStoryChapterListAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<StoryChapterListModel>>($"stories/{storyId}/chapters"));

        public Task<AuthorizedModel<StoryModel>> GetStoryAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<AuthorizedModel<StoryModel>>($"stories/{storyId}"));

        public Task<StoryModel> CreateStoryAsync(StoryModel model)
            => faultHandler.Wrap(http.PostAsJsonAsync<StoryModel, StoryModel>("stories", model));

        public Task UpdateStoryAsync(StoryModel model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"stories/{model.Id}", model));

        public Task DeleteStoryAsync(string storyId)
            => faultHandler.Wrap(http.DeleteAsync($"stories/{storyId}"));

        public Task<EntryStoryModel> GetEntryStoryAsync(string entryId)
            => faultHandler.Wrap(http.GetFromJsonAsync<EntryStoryModel>($"entries/{entryId}/story"));

        public Task UpdateEntryStoryAsync(string entryId, EntryStoryUpdateModel model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"entries/{entryId}/story", model));

        public Task<List<StoryEntryModel>> GetStoryEntryListAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<StoryEntryModel>>($"stories/{storyId}/entries"));

        public Task<List<StoryEntryModel>> GetStoryChapterEntryListAsync(string storyId, string chapterId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<StoryEntryModel>>($"stories/{storyId}/chapters/{chapterId}/entries"));

        public Task<SearchResponse> SearchAsync(string query, int offset = 0)
        {
            string url = "search";
            url = QueryHelpers.AddQueryString(url, "q", query);

            if (offset > 0)
                url = QueryHelpers.AddQueryString(url, "offset", offset.ToString());

            return faultHandler.Wrap(http.GetFromJsonAsync<SearchResponse>(url));
        }

        public Task<List<BeingListModel>> GetBeingListAsync()
        {
            var models = new List<BeingListModel>()
            {
                new BeingListModel()
                {
                    Id = "ae08c8cf-0dc8-4123-8c53-55e0c0982f51",
                    Name = "Ivy",
                    Icon = "crow"
                },
                new BeingListModel()
                {
                    Id = "22c011fd-2051-4ad5-9f73-c20ab01ec763",
                    Name = "Sorin",
                    Icon = "dove"
                },
                new BeingListModel()
                {
                    Id = "77ff59de-4d54-49fd-953e-eaad50bd6727",
                    Name = "Mycroft",
                    Icon = "dog"
                }
            };

            foreach (var model in models)
            {
                if (model.UserId == null)
                {
                    model.UserId = "858dd45a-c58e-4cc2-8b1e-06be21747629";
                    model.UserName = "tester";
                }
            }

            return Task.FromResult(models);
        }

        public Task<VersionModel> GetVersionAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<VersionModel>($"entries/version"));
    }
}
