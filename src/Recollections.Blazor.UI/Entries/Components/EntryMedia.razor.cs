using Microsoft.AspNetCore.Components;
using Neptuo.Logging;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Models;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class EntryMedia
    {
        private bool hasSourceChanged;
        private string url;
        private string previousUrl;

        protected bool HasUrl { get; private set; }
        protected bool IsLoaded { get; private set; }
        protected bool IsLoadingNotFound { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected ImageInterop ImageInterop { get; set; }

        [Inject]
        protected ILog<EntryMedia> Log { get; set; }

        [CascadingParameter]
        protected UserState UserState { get; set; }

        [Parameter]
        [CascadingParameter]
        public EntryModel Entry { get; set; }

        [Parameter]
        public string EntryId { get; set; }

        [Parameter]
        public ImageModel Image { get; set; }

        [Parameter]
        public VideoModel Video { get; set; }

        [Parameter]
        public MediaType Type { get; set; } = MediaType.Thumbnail;

        [Parameter]
        public RenderFragment ThumbnailContent { get; set; }

        [Parameter]
        public EventCallback OnClick { get; set; }

        [Parameter]
        public bool ClickToPlay { get; set; }

        protected ElementReference Element { get; set; }

        protected bool IsVideoLoading = false;
        protected bool IsVideoContent = false;

        protected string GetLinkUrl()
        {
            if (Entry != null)
                EntryId = Entry.Id;

            if (EntryId == null || Image == null)
                return null;

            return Navigator.UrlImageDetail(EntryId, Image.Id);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            previousUrl = url;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (IsVideoContent)
                return;

            string mediaUrl = null;
            if (Image != null)
                mediaUrl = FindImageUrl(Image);
            else if (Video != null)
                mediaUrl = FindImageUrl(Video);

            if (mediaUrl != null)
            {
                if (previousUrl != mediaUrl)
                {
                    IsLoadingNotFound = false;
                    IsLoaded = false;
                    previousUrl = mediaUrl;
                    url = mediaUrl;
                    hasSourceChanged = true;
                    HasUrl = true;
                }
            }
            else
            {
                url = null;
                HasUrl = false;
                IsLoaded = false;
            }
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (hasSourceChanged && url != null)
            {
                hasSourceChanged = false;
                await LoadFromUrlAsync(url, IsVideoContent ? Video?.ContentType : null);
            }
        }

        private async Task LoadFromUrlAsync(string mediaUrl, string contentType)
        {
            string absoluteUrl = Api.GetMediaUrl(mediaUrl);
            Log.Debug("Downloading media from {0}", absoluteUrl);

            int status;
            try
            {
                status = await ImageInterop.SetFromUrlAsync(Element, absoluteUrl, contentType, UserState?.BearerToken);
            }
            catch (Exception e)
            {
                HandleMediaLoadFailure(0, $"Exception during media download: {e.Message}");
                return;
            }
            finally
            {
                IsVideoLoading = false;
            }

            if (!IsSuccessfulStatus(status))
            {
                HandleMediaLoadFailure(status, $"Media download failed with status '{status}'");
                return;
            }

            Log.Debug("Media downloaded successfully");
            IsLoaded = true;
            IsLoadingNotFound = false;
            StateHasChanged();
        }

        private static bool IsSuccessfulStatus(int status)
            => status >= (int)HttpStatusCode.OK && status < 300;

        private void HandleMediaLoadFailure(int status, string message)
        {
            Log.Debug(message);
            IsLoadingNotFound = status == (int)HttpStatusCode.NotFound;
            IsLoaded = false;
            HasUrl = false;
            StateHasChanged();
        }

        private string FindImageUrl(IMediaUrlList media, MediaType? type = null)
        {
            switch (type ?? Type)
            {
                case MediaType.Original:
                    return media.Original?.Url;
                case MediaType.Preview:
                    return media.Preview?.Url;
                case MediaType.Thumbnail:
                    return media.Thumbnail?.Url;
                default:
                    throw Ensure.Exception.NotSupported(Type);
            }
        }

        private void DownloadVideo()
        {
            if (Video == null || IsVideoLoading)
                return;

            IsVideoLoading = true;
            IsVideoContent = true;
            url = FindImageUrl(Video, MediaType.Original);
            previousUrl = url;
            hasSourceChanged = true;
            HasUrl = true;
            IsLoaded = false;
            StateHasChanged();
        }
    }
}
