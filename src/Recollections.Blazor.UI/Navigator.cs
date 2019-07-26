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
        private readonly IUriHelper uri;
        private readonly IJSRuntime jsRuntime;

        public Navigator(IUriHelper uri, IJSRuntime jsRuntime)
        {
            Ensure.NotNull(uri, "uri");
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.uri = uri;
            this.jsRuntime = jsRuntime;
        }

        public Task<bool> AskAsync(string message)
            => jsRuntime.InvokeAsync<bool>("window.confirm", message);

        public Task MessageAsync(string message)
            => jsRuntime.InvokeAsync<bool>("window.alert", message);

        public string UrlRegister()
            => "/register";

        public void OpenRegister()
            => uri.NavigateTo(UrlRegister());

        public string UrlTimeline()
            => "/";

        public void OpenTimeline()
            => uri.NavigateTo(UrlTimeline());

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
