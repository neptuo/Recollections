using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public class EntryImageModel : ComponentBase
    {
        private string previousUrl;

        protected string Url { get; private set; } = "/img/thumbnail-placeholder.png";

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected Api Api { get; set; }

        [Parameter]
        [CascadingParameter]
        public EntryModel Entry { get; set; }

        [Parameter]
        public ImageModel Image { get; set; }

        [Parameter]
        public string PlaceHolder { get; set; }

        protected string GetLinkUrl()
        {
            if (Entry == null || Image == null)
                return null;

            return Navigator.UrlImageDetail(Entry.Id, Image.Id);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            previousUrl = Url;
        }

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (previousUrl != Image.Thumbnail)
            {
                previousUrl = Image.Thumbnail;

                byte[] content = await Api.GetImageDataAsync(Image.Thumbnail);
                Url = "data:image/png;base64," + Convert.ToBase64String(content);
            }
        }
    }
}
