using Microsoft.AspNetCore.Components;
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
    public partial class EntryImage
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
        protected ILog<EntryImage> Log { get; set; }

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
        public ImageType ImageType { get; set; } = ImageType.Thumbnail;

        [Parameter]
        public string PlaceHolder { get; set; }

        [Parameter]
        public string PlaceHolderCssClass { get; set; }

        [Parameter]
        public EntryImagePlaceHolderState PlaceHolderState { get; set; }

        [Parameter]
        public RenderFragment ThumbnailContent { get; set; }

        [Parameter]
        public EventCallback OnClick { get; set; }

        protected ElementReference Element { get; set; }

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

            string imageUrl = null;
            if (Image != null)
                imageUrl = FindImageUrl();
            else if (Video != null)
                imageUrl = Video.Thumbnail?.Url;

            if (imageUrl != null)
            {
                if (previousUrl != imageUrl)
                {
                    IsLoadingNotFound = false;
                    previousUrl = imageUrl;
                    url = imageUrl;
                    hasSourceChanged = true;
                    _ = LoadImageDataAsync(imageUrl).ContinueWith(_ => StateHasChanged());
                }

                return;
            }
            else
            {
                Content = null;
            }
        }

        private async Task LoadImageDataAsync(string imageUrl)
        {
            using (ExceptionPanelSuppression.Enter<HttpRequestException>(e => e.StatusCode == HttpStatusCode.NotFound))
            {
                try
                {
                    Content = await Api.GetImageDataAsync(imageUrl);
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
                await ImageInterop.SetAsync(Element, Content);
            }
        }

        private string FindImageUrl()
        {
            switch (ImageType)
            {
                case ImageType.Original:
                    return Image.Original?.Url;
                case ImageType.Preview:
                    return Image.Preview?.Url;
                case ImageType.Thumbnail:
                    return Image.Thumbnail?.Url;
                default:
                    throw Ensure.Exception.NotSupported(ImageType);
            }
        }
    }
}