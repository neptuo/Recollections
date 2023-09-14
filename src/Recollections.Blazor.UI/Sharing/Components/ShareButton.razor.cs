using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Sharing.Components
{
    public partial class ShareButton
    {
        private IApi api;

        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected WindowInterop Interop { get; set; }

        [Parameter]
        public string EntryId { get; set; }

        [Parameter]
        public string StoryId { get; set; }

        [Parameter]
        public string BeingId { get; set; }

        protected bool AreItemsLoading { get; set; }
        protected Modal Modal { get; set; }
        protected List<ShareModel> Items { get; set; }
        protected bool HasPublic { get; set; }

        protected bool IsCopiedToClipboard { get; set; } = false;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (!String.IsNullOrEmpty(EntryId))
                api = new EntryApi(Api, EntryId);
            else if (!String.IsNullOrEmpty(StoryId))
                api = new StoryApi(Api, StoryId);
            else if (!String.IsNullOrEmpty(BeingId))
                api = new BeingApi(Api, BeingId);
            else
                throw Ensure.Exception.InvalidOperation("One of 'EntryId', 'StoryId' or 'BeingId' must be provided.");
        }

        private async Task LoadAsync()
        {
            AreItemsLoading = true;
            Items = await api.GetListAsync();
            HasPublic = Items.Any(s => s.UserName == "public");
            AreItemsLoading = false;

            StateHasChanged();
        }

        protected void OnShow()
        { 
            Modal.Show();
            _ = LoadAsync();
        }

        private void PermissionChanged(ShareModel model, ChangeEventArgs args)
        {
            if (args.Value == null)
                model.Permission = null;
            else
                model.Permission = (Permission)args.Value;
        }

        protected async Task SaveAsync() 
        {
            await api.SaveAsync(Items);
            Modal.Hide();
        }

        interface IApi
        {
            Task<List<ShareModel>> GetListAsync();
            Task SaveAsync(List<ShareModel> model);
        }

        class EntryApi : IApi
        {
            private readonly Api api;
            private readonly string entryId;

            public EntryApi(Api api, string entryId)
            {
                Ensure.NotNull(api, "api");
                Ensure.NotNull(entryId, "entryId");
                this.api = api;
                this.entryId = entryId;
            }

            public Task SaveAsync(List<ShareModel> model)
                => api.SaveEntryAsync(entryId, model);

            public Task<List<ShareModel>> GetListAsync()
                => api.GetEntryListAsync(entryId);
        }

        class StoryApi : IApi
        {
            private readonly Api api;
            private readonly string storyId;

            public StoryApi(Api api, string storyId)
            {
                Ensure.NotNull(api, "api");
                Ensure.NotNull(storyId, "storyId");
                this.api = api;
                this.storyId = storyId;
            }

            public Task SaveAsync(List<ShareModel> model)
                => api.SaveStoryAsync(storyId, model);

            public Task<List<ShareModel>> GetListAsync()
                => api.GetStoryListAsync(storyId);
        }

        class BeingApi : IApi
        {
            private readonly Api api;
            private readonly string beingId;

            public BeingApi(Api api, string beingId)
            {
                Ensure.NotNull(api, "api");
                Ensure.NotNull(beingId, "beingId");
                this.api = api;
                this.beingId = beingId;
            }

            public Task SaveAsync(List<ShareModel> model)
                => api.SaveBeingAsync(beingId, model);

            public Task<List<ShareModel>> GetListAsync()
                => api.GetBeingListAsync(beingId);
        }
    }
}
