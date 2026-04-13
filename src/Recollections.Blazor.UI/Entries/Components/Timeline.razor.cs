using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.JSInterop;
using Neptuo.Logging;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Components
{
    public partial class Timeline(Navigator Navigator, NavigationManager NavigationManager, IJSRuntime JSRuntime, UiOptions UiOptions, Api Api, ILog<Timeline> Log)
    {
        private readonly Dictionary<string, List<MediaModel>> galleryMediaByEntryId = [];
        private readonly HashSet<string> galleryMediaLoading = [];

        [Parameter]
        public RenderFragment BeforeContent { get; set; }

        [Parameter]
        public string NoMoreText { get; set; }

        [Parameter]
        public bool AllowMore { get; set; } = true;

        [Parameter]
        public bool ShowStoryInfo { get; set; } = true;

        [Parameter]
        public bool ShowYearSeparators { get; set; } = false;

        [Parameter]
        public bool Collapsible { get; set; } = false;

        [Parameter]
        public List<EntryListModel> Data { get; set; }

        [Parameter]
        public Func<int, int?, Task<PageableList<EntryListModel>>> DataGetter { get; set; }

        [Parameter]
        public EventCallback<EntryListModel> OnClick { get; set; }

        private int offset;
        private Task loadAsyncFromParametersSet;
        private string scrollToEntryId;
        private bool clearRestoredPositionAfterRender;
        private string currentGalleryEntryId;

        protected List<EntryListModel> Entries { get; } = [];
        protected Gallery Gallery { get; set; }
        protected List<GalleryModel> GalleryItems { get; } = [];
        protected bool HasMore { get; private set; }
        protected bool IsLoading { get; private set; } = true;
        protected bool IsCollapsed { get; set; } = false;

        protected async override Task OnInitializedAsync()
        {
            Log.Debug("Init");

            await base.OnInitializedAsync();
        }

        private TimelinePosition FindPositionFromHistoryEntry()
        {
            Log.Debug($"Finding timeline position from history entry, state='{NavigationManager.HistoryEntryState}'");
            return PageHistoryState.Parse(NavigationManager.HistoryEntryState).Timeline;
        }

        private void QueuePositionRestore(TimelinePosition position)
        {
            if (position == null)
                return;

            clearRestoredPositionAfterRender = true;
            scrollToEntryId = position.EntryId;
        }

        protected async override Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (Data != null)
            {
                AllowMore = false;
                Entries.Clear();
                Entries.AddRange(Data);
                Log.Debug($"Got parameter Data '{Data.Count}'");

                var position = FindPositionFromHistoryEntry();
                QueuePositionRestore(position);
            }
            else if (Entries.Count == 0)
            {
                if (loadAsyncFromParametersSet == null)
                {
                    Log.Debug("LoadAsync");
                    loadAsyncFromParametersSet = LoadAsync().ContinueWith(t => { loadAsyncFromParametersSet = null; StateHasChanged(); });
                }
                else
                {
                    Log.Debug("LoadAsync skipped due to pending load operation");
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (loadAsyncFromParametersSet == null && (scrollToEntryId != null || clearRestoredPositionAfterRender))
            {
                var entryId = scrollToEntryId;
                var shouldClearPosition = clearRestoredPositionAfterRender;
                scrollToEntryId = null;
                clearRestoredPositionAfterRender = false;

                if (!string.IsNullOrEmpty(entryId))
                {
                    Log.Debug($"Scrolling to entry '{entryId}'");
                    await JSRuntime.InvokeVoidAsync("Timeline.ScrollToEntry", entryId);
                }

                if (shouldClearPosition)
                {
                    Log.Debug("Clearing consumed timeline position from history state");
                    await JSRuntime.InvokeVoidAsync("Timeline.ClearPosition");
                }
            }
        }

        private async Task LoadAsync()
        {
            Ensure.NotNull(DataGetter, "DataGetter");

            try
            {
                IsLoading = true;

                int? count = null;

                Log.Debug($"Loading timeline with offset '{offset}' and count '{count}', current count '{Entries.Count}'");
                if (Entries.Count == 0)
                {
                    var position = FindPositionFromHistoryEntry();
                    if (position != null)
                    {
                        if (position.Offset > 0)
                            count = position.Offset;

                        QueuePositionRestore(position);
                        Log.Debug($"Restoring timeline position: offset={position.Offset}, entryId={position.EntryId}");
                    }
                }

                PageableList<EntryListModel> response = await DataGetter(offset, count);

                Entries.AddRange(response.Models);
                HasMore = response.HasMore;
                offset = Entries.Count;

                Log.Debug($"Loaded '{response.Models.Count}' ('{(HasMore ? "has more" : "end of stream")}'), total so far '{Entries.Count}'");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public Task LoadMoreAsync()
            => LoadAsync();

        private async Task OnEntryClicked(EntryListModel entry)
        {
            if (OnClick.HasDelegate)
                await OnClick.InvokeAsync(entry);
        }

        protected async Task OpenGalleryAsync(EntryListModel entry, int index)
        {
            Ensure.NotNull(entry, "entry");
            if (Gallery == null)
                return;

            currentGalleryEntryId = entry.Id;
            if (!galleryMediaByEntryId.TryGetValue(entry.Id, out List<MediaModel> media) || media.Count == 0)
            {
                media = entry.PreviewMedia ?? [];
                galleryMediaByEntryId[entry.Id] = media;
            }

            UpdateGalleryItems(media);

            await Gallery.OpenAsync(index);
            _ = EnsureFullMediaAsync(entry);
        }

        private async Task EnsureFullMediaAsync(EntryListModel entry)
        {
            int totalMediaCount = entry.ImageCount + entry.VideoCount;
            if (totalMediaCount <= (entry.PreviewMedia?.Count ?? 0) || !galleryMediaLoading.Add(entry.Id))
                return;

            try
            {
                List<MediaModel> media = await Api.GetMediaAsync(entry.Id);
                galleryMediaByEntryId[entry.Id] = media;

                if (currentGalleryEntryId == entry.Id)
                {
                    UpdateGalleryItems(media);
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Unable to load full media for entry '{entry.Id}': {ex}");
            }
            finally
            {
                galleryMediaLoading.Remove(entry.Id);
            }
        }

        private void UpdateGalleryItems(IEnumerable<MediaModel> media)
        {
            GalleryItems.Clear();
            foreach (MediaModel item in media)
            {
                if (item.Image != null)
                {
                    GalleryItems.Add(new GalleryModel
                    {
                        Type = "image",
                        Title = item.Image.Name,
                        Width = item.Image.Preview.Width,
                        Height = item.Image.Preview.Height
                    });
                }
                else if (item.Video != null)
                {
                    GalleryItems.Add(new GalleryModel
                    {
                        Type = "video",
                        Title = item.Video.Name,
                        Width = item.Video.Preview.Width,
                        Height = item.Video.Preview.Height,
                        ContentType = item.Video.ContentType
                    });
                }
            }
        }

        protected Task<Stream> OnGetMediaDataAsync(int index, string type)
        {
            if (currentGalleryEntryId == null || !galleryMediaByEntryId.TryGetValue(currentGalleryEntryId, out List<MediaModel> media) || index >= media.Count)
                return Task.FromResult<Stream>(null);

            MediaModel item = media[index];
            if (item.Image != null)
                return Api.GetMediaDataAsync(item.Image.Preview.Url);

            if (item.Video != null)
                return Api.GetMediaDataAsync(type == "original" ? item.Video.Original.Url : item.Video.Preview.Url);

            return Task.FromResult<Stream>(null);
        }

        protected async Task OnGalleryOpenInfoAsync(int index)
        {
            if (currentGalleryEntryId == null || !galleryMediaByEntryId.TryGetValue(currentGalleryEntryId, out List<MediaModel> media) || index < 0 || index >= media.Count)
                return;

            if (Gallery != null)
                await Gallery.CloseAsync();

            MediaModel item = media[index];
            if (item.Image != null)
                Navigator.OpenImageDetail(currentGalleryEntryId, item.Image.Id);
            else if (item.Video != null)
                Navigator.OpenVideoDetail(currentGalleryEntryId, item.Video.Id);
        }
    }
}
