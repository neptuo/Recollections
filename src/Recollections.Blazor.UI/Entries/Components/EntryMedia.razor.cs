using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Neptuo.Logging;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class EntryMedia
    {
        private bool hasSourceChanged;
        private string url;
        private string previousUrl;

        protected Stream Content { get; private set; }
        protected bool IsLoadingNotFound { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected ImageInterop ImageInterop { get; set; }

        [Inject]
        protected ILog<EntryMedia> Log { get; set; }

        [Inject]
        protected ExceptionPanelSuppression ExceptionPanelSuppression { get; set; }

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
        public string PlaceHolder { get; set; }

        [Parameter]
        public string PlaceHolderCssClass { get; set; }

        [Parameter]
        public EntryMediaPlaceHolderState PlaceHolderState { get; set; }

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

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (!IsVideoContent)
            {
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
                        previousUrl = mediaUrl;
                        url = mediaUrl;
                        hasSourceChanged = true;
                        _ = LoadMediaDataAsync(mediaUrl).ContinueWith(_ => StateHasChanged());
                    }

                    return;
                }
                else
                {
                    Content = null;
                }
            }
        }

        private async Task LoadMediaDataAsync(string mediaUrl)
        {
            using (ExceptionPanelSuppression.Enter<HttpRequestException>(e => e.StatusCode == HttpStatusCode.NotFound))
            {
                try
                {
                    Log.Debug("Downloading media from {0}", mediaUrl);
                    Content = await Api.GetMediaDataAsync(mediaUrl);
                    Log.Debug("Media downloaded successfully");
                }
                catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
                {
                    Log.Debug("Exception during image download");
                    IsLoadingNotFound = true;
                }
            }
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (Content != null && hasSourceChanged)
            {
                hasSourceChanged = false;
                await ImageInterop.SetAsync(Element, Content, IsVideoContent ? Video?.ContentType : null);
            }
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
            LoadMediaDataAsync(FindImageUrl(Video, MediaType.Original)).ContinueWith(_ =>
            {
                IsVideoLoading = false;
                IsVideoContent = true;
                hasSourceChanged = true;
                StateHasChanged();
            });
        }
    }
}