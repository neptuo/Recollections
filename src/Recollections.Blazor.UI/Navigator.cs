using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class Navigator
    {
        private readonly NavigationManager uri;
        private readonly IJSRuntime jsRuntime;

        public Navigator(NavigationManager uri, IJSRuntime jsRuntime)
        {
            Ensure.NotNull(uri, "uri");
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.uri = uri;
            this.jsRuntime = jsRuntime;
        }

        public ValueTask ReloadAsync()
            => jsRuntime.InvokeVoidAsync("window.location.reload");

        public ValueTask<bool> AskAsync(string message)
            => jsRuntime.InvokeAsync<bool>("window.confirm", message);

        public ValueTask<bool> MessageAsync(string message)
            => jsRuntime.InvokeAsync<bool>("window.alert", message);

        public string UrlRegister()
            => "/register";

        public void OpenRegister()
            => uri.NavigateTo(UrlRegister());

        public string UrlTimeline()
            => "/";

        public void OpenTimeline()
            => uri.NavigateTo(UrlTimeline());

        public string UrlMap()
            => "/map";

        public void OpenMap()
            => uri.NavigateTo(UrlMap());

        public string UrlStories()
            => "/stories";

        public void OpenStories()
            => uri.NavigateTo(UrlStories());

        public string UrlStoryDetail(string storyId)
            => $"/story/{storyId}";

        public void OpenStoryDetail(string storyId)
            => uri.NavigateTo(UrlStoryDetail(storyId));

        public string UrlEntryDetail(string entryId)
            => $"/entry/{entryId}";

        public void OpenEntryDetail(string entryId)
            => uri.NavigateTo(UrlEntryDetail(entryId));

        public string UrlImageDetail(string entryId, string imageId)
            => $"/entry/{entryId}/image/{imageId}";

        public void OpenImageDetail(string entryId, string imageId)
            => uri.NavigateTo(UrlImageDetail(entryId, imageId));
    }
}
