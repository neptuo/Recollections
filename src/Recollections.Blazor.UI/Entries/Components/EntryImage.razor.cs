using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Entries.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class EntryImage
    {
        public const string PlaceholderUrl = "/img/thumbnail-placeholder.png";

        private string previousUrl;

        protected string Url { get; private set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        [Parameter]
        [CascadingParameter]
        public EntryModel Entry { get; set; }

        [Parameter]
        public string EntryId { get; set; }

        [Parameter]
        public ImageModel Image { get; set; }

        [Parameter]
        public ImageType ImageType { get; set; } = ImageType.Thumbnail;

        [Parameter]
        public string PlaceHolder { get; set; }

        [Parameter]
        public string PlaceHolderCssClass { get; set; }

        [Parameter]
        public RenderFragment ThumbnailContent { get; set; }

        [Parameter]
        public EventCallback OnClick { get; set; }

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

            previousUrl = Url;
        }

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (Image != null)
            {
                string imageUrl = GetImageUrl();
                if (previousUrl != imageUrl)
                {
                    previousUrl = imageUrl;
                    Url = PlaceholderUrl;

                    byte[] content = await Api.GetImageDataAsync(imageUrl);
                    Url = "data:image/png;base64," + Convert.ToBase64String(content);
                }
            }
            else
            {
                Url = PlaceholderUrl;
            }

            if (String.IsNullOrEmpty(Url))
                Url = PlaceholderUrl;
        }

        private string GetImageUrl()
        {
            switch (ImageType)
            {
                case ImageType.Original:
                    return Image.Original.Url;
                case ImageType.Preview:
                    return Image.Preview.Url;
                case ImageType.Thumbnail:
                    return Image.Thumbnail.Url;
                default:
                    throw Ensure.Exception.NotSupported(ImageType);
            }
        }
    }
}
