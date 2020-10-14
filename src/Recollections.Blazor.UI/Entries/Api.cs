using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Neptuo;
using Neptuo.Activators;
using Neptuo.Exceptions.Handlers;
using Neptuo.Logging;
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
            => faultHandler.Wrap(http.PostJsonAsync("entries", model));

        public Task UpdateEntryAsync(EntryModel model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"entries/{model.Id}", model));

        public Task DeleteEntryAsync(string entryId)
            => faultHandler.Wrap(http.DeleteAsync($"entries/{entryId}"));

        public Task<(EntryModel, Permission)> GetEntryAsync(string entryId)
            => faultHandler.Wrap(GetEntryPrivateAsync(entryId));

        private async Task<(EntryModel, Permission)> GetEntryPrivateAsync(string entryId)
        {
            HttpResponseMessage responseMessage = await http.GetAsync($"entries/{entryId}");
            if (responseMessage.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            responseMessage.EnsureSuccessStatusCode();

            var model = await responseMessage.Content.ReadFromJsonAsync<EntryModel>();
            var permission = GetPermissionFromResponse(responseMessage);

            return (model, permission);
        }

        private static Permission GetPermissionFromResponse(HttpResponseMessage responseMessage)
        {
            Permission permission = Permission.Write;
            if (responseMessage.Headers.TryGetValues(PermissionHeader.Name, out var permissionHeaderValues))
            {
                if (!Enum.TryParse(permissionHeaderValues.FirstOrDefault(), out permission))
                    permission = Permission.Write;
            }

            return permission;
        }

        public Task<List<ImageModel>> GetImagesAsync(string entryId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<ImageModel>>($"entries/{entryId}/images"));

        public Task<(ImageModel, Permission)> GetImageAsync(string entryId, string imageId)
            => faultHandler.Wrap(GetImagePrivateAsync(entryId, imageId));
        
        private async Task<(ImageModel, Permission)> GetImagePrivateAsync(string entryId, string imageId)
        {
            HttpResponseMessage responseMessage = await http.GetAsync($"entries/{entryId}/images/{imageId}");
            responseMessage.EnsureSuccessStatusCode();

            var model = await responseMessage.Content.ReadFromJsonAsync<ImageModel>();
            var permission = GetPermissionFromResponse(responseMessage);

            return (model, permission);
        }

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

        public Task<StoryModel> GetStoryAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<StoryModel>($"stories/{storyId}"));

        public Task<StoryModel> CreateStoryAsync(StoryModel model)
            => faultHandler.Wrap(http.PostJsonAsync($"stories", model));

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

        public Task<VersionModel> GetVersionAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<VersionModel>($"entries/version"));
    }
}
