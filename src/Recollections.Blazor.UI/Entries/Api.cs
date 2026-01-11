using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Neptuo.Activators;
using Neptuo.Recollections.Commons.Exceptions;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Beings;
using Neptuo.Recollections.Entries.Stories;
using Neptuo.Recollections.Sharing;
using System;
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

        private async Task<bool> SaveAsync<T>(string url, T model) 
        {
            try
            {
                var response = await http.PutAsJsonAsync(url, model);
                if (response.IsSuccessStatusCode)
                    return true;
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                    return false;
                else
                    response.EnsureSuccessStatusCode();
            }
            catch (UnauthorizedAccessException) 
            {
                return false;
            }

            return false;
        }

        public Task<PageableList<EntryListModel>> GetTimelineListAsync(int? offset)
            => faultHandler.Wrap(http.GetFromJsonAsync<PageableList<EntryListModel>>($"timeline/list{(offset != null && offset > 0 ? $"?offset={offset}" : null)}"));

        public Task<PageableList<EntryListModel>> GetTimelineListAsync(string userId, int? offset)
            => faultHandler.Wrap(http.GetFromJsonAsync<PageableList<EntryListModel>>($"profiles/{userId}/timeline/list{(offset != null && offset > 0 ? $"?offset={offset}" : null)}"));
        
        public Task<List<MapEntryModel>> GetMapListAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<List<MapEntryModel>>("map/list"));

        public Task<List<MapSearchModel>> GetGeoLocateListAsync(string query)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<MapSearchModel>>(QueryHelpers.AddQueryString("map/geolocate", "q", query)));

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

        public Task<List<MediaModel>> GetMediaAsync(string entryId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<MediaModel>>($"entries/{entryId}/media"));

        public Task<AuthorizedModel<ImageModel>> GetImageAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.GetFromJsonAsync<AuthorizedModel<ImageModel>>($"entries/{entryId}/images/{imageId}"));

        public Task<Stream> GetMediaDataAsync(string url)
            => faultHandler.Wrap(http.GetStreamAsync((settings.BaseUrl + url).Replace("api/api", "api")));

        public Task UpdateImageAsync(string entryId, ImageModel model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"entries/{entryId}/images/{model.Id}", model));

        public Task DeleteImageAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.DeleteAsync($"entries/{entryId}/images/{imageId}"));

        public Task SetImageLocationFromOriginalAsync(string entryId, string imageId)
            => faultHandler.Wrap(http.PostAsync($"entries/{entryId}/images/{imageId}/set-location-from-original", new StringContent(String.Empty)));

        public string ImageUploadUrl(string entryId) => $"{settings.BaseUrl}entries/{entryId}/images";

        public string MediaUploadUrl(string entryId) => $"{settings.BaseUrl}entries/{entryId}/media";

        public Task<List<StoryListModel>> GetStoryListAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<List<StoryListModel>>("stories"));

        public Task<List<StoryChapterListModel>> GetStoryChapterListAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<StoryChapterListModel>>($"stories/{storyId}/chapters"));

        public Task<List<EntryImagesModel>> GetStoryImagesAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<EntryImagesModel>>($"stories/{storyId}/images"));

        public Task<List<EntryMediaModel>> GetStoryMediaAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<EntryMediaModel>>($"stories/{storyId}/media"));

        public Task<List<MapEntryModel>> GetStoryMapAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<MapEntryModel>>($"stories/{storyId}/map"));

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

        public Task<bool> UpdateEntryStoryAsync(string entryId, EntryStoryUpdateModel model)
            => faultHandler.Wrap(SaveAsync($"entries/{entryId}/story", model));

        public Task<PageableList<EntryListModel>> GetStoryTimelineAsync(string storyId)
            => faultHandler.Wrap(http.GetFromJsonAsync<PageableList<EntryListModel>>($"stories/{storyId}/timeline"));

        public Task<PageableList<EntryListModel>> GetStoryChapterTimelineAsync(string storyId, string chapterId)
            => faultHandler.Wrap(http.GetFromJsonAsync<PageableList<EntryListModel>>($"stories/{storyId}/chapters/{chapterId}/timeline"));
        
        public Task<List<BeingListModel>> GetBeingListAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<List<BeingListModel>>("beings"));

        public Task<AuthorizedModel<BeingModel>> GetBeingAsync(string beingId)
            => faultHandler.Wrap(http.GetFromJsonAsync<AuthorizedModel<BeingModel>>($"beings/{beingId}"));

        public Task<BeingModel> CreateBeingAsync(BeingModel model)
            => faultHandler.Wrap(http.PostAsJsonAsync<BeingModel, BeingModel>($"beings", model));

        public Task UpdateBeingAsync(BeingModel model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"beings/{model.Id}", model));

        public Task DeleteBeingAsync(string beingId)
            => faultHandler.Wrap(http.DeleteAsync($"beings/{beingId}"));

        public Task<List<EntryBeingModel>> GetEntryBeingsAsync(string entryId)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<EntryBeingModel>>($"entries/{entryId}/beings"));

        public Task UpdateEntryBeingsAsync(string entryId, List<string> beingIds)
            => faultHandler.Wrap(http.PutAsJsonAsync($"entries/{entryId}/beings", beingIds));

        public Task<PageableList<EntryListModel>> GetBeingTimelineAsync(string beingId, int? offset)
            => faultHandler.Wrap(http.GetFromJsonAsync<PageableList<EntryListModel>>($"beings/{beingId}/timeline{(offset != null && offset > 0 ? $"?offset={offset}" : null)}"));

        public Task<PageableList<EntryListModel>> SearchAsync(string query, int offset = 0)
        {
            string url = "search";
            url = QueryHelpers.AddQueryString(url, "q", query);

            if (offset > 0)
                url = QueryHelpers.AddQueryString(url, "offset", offset.ToString());

            return faultHandler.Wrap(http.GetFromJsonAsync<PageableList<EntryListModel>>(url));
        }

        public Task<List<EntryListModel>> GetYearEntryListAsync(int year)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<EntryListModel>>($"calendar/{year}"));

        public Task<List<EntryListModel>> GetMonthEntryListAsync(int year, int month)
            => faultHandler.Wrap(http.GetFromJsonAsync<List<EntryListModel>>($"calendar/{year}/{month}"));
            
        public Task<VersionModel> GetVersionAsync()
            => faultHandler.Wrap(http.GetFromJsonAsync<VersionModel>($"entries/version"));

        public Task<Stream> GetTileAsync(string type, int x, int y, int z)
            => faultHandler.Wrap(http.GetStreamAsync($"maptiles/{type}/256/{z}/{x}/{y}"));

        public Task<AuthorizedModel<VideoModel>> GetVideoAsync(string entryId, string videoId)
            => faultHandler.Wrap(http.GetFromJsonAsync<AuthorizedModel<VideoModel>>($"entries/{entryId}/videos/{videoId}"));

        public Task UpdateVideoAsync(string entryId, VideoModel model)
            => faultHandler.Wrap(http.PutAsJsonAsync($"entries/{entryId}/videos/{model.Id}", model));

        public Task DeleteVideoAsync(string entryId, string videoId)
            => faultHandler.Wrap(http.DeleteAsync($"entries/{entryId}/videos/{videoId}"));

        public Task SetVideoLocationFromOriginalAsync(string entryId, string videoId)
            => faultHandler.Wrap(http.PostAsync($"entries/{entryId}/videos/{videoId}/set-location-from-original", new StringContent(String.Empty)));
    }
}
