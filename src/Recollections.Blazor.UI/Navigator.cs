using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public class Navigator : IDisposable
    {
        private readonly NavigationManager uri;
        private readonly IJSRuntime jsRuntime;
        private Dictionary<string, StringValues> queryString;

        public event Action<string> LocationChanged;

        public Navigator(NavigationManager uri, IJSRuntime jsRuntime)
        {
            Ensure.NotNull(uri, "uri");
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.uri = uri;
            this.jsRuntime = jsRuntime;

            uri.LocationChanged += OnLocationChanged;
        }

        public void Dispose()
        {
            uri.LocationChanged -= OnLocationChanged;
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            queryString = null;
            LocationChanged?.Invoke(e.Location);
        }

        public ValueTask ReloadAsync()
            => jsRuntime.InvokeVoidAsync("window.location.reload");

        public Dictionary<string, StringValues> GetQueryString()
        {
            if (queryString == null)
            {
                var absolute = uri.ToAbsoluteUri(uri.Uri);
                queryString = QueryHelpers.ParseQuery(absolute.Query);
            }

            return queryString;
        }

        public string FindQueryParameter(string name)
        {
            if (GetQueryString().TryGetValue(name, out var value))
                return value;

            return null;
        }

        public ValueTask<bool> AskAsync(string message)
            => jsRuntime.InvokeAsync<bool>("window.confirm", message);

        public ValueTask<bool> MessageAsync(string message)
            => jsRuntime.InvokeAsync<bool>("window.alert", message);

        public string UrlGithubRepository()
            => "https://github.com/neptuo/Recollections";

        public string UrlGithubRepositoryIssuesNew()
            => "https://github.com/neptuo/Recollections/issues/new";

        public string UrlAbout()
            => "/about";

        public void OpenAbout()
            => uri.NavigateTo(UrlAbout());

        public string UrlLogin()
            => "/login";

        public void OpenLogin()
            => uri.NavigateTo(UrlLogin());

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

        public string UrlCalendar(int? year = null, int? month = null)
        {
            string url = "/calendar";
            if (year != null)
            {
                url += $"/{year}";
                if (month != null)
                    url += $"/{month}";
            }

            return url;
        }

        public void OpenCalendar(int? year = null, int? month = null)
            => uri.NavigateTo(UrlCalendar(year, month));

        public string UrlSearch(string query = null)
        {
            string url = "/search";
            if (!String.IsNullOrEmpty(query))
                url = QueryHelpers.AddQueryString(url, "q", query);

            return url;
        }

        public void OpenSearch(string query = null)
            => uri.NavigateTo(UrlSearch(query));

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

        public string UrlBeings()
            => "/beings";

        public void OpenBeings()
            => uri.NavigateTo(UrlBeings());

        public string UrlBeingDetail(string beingId)
            => $"/being/{beingId}";

        public void OpenBeingDetail(string beingId)
            => uri.NavigateTo(UrlBeingDetail(beingId));
    }
}
