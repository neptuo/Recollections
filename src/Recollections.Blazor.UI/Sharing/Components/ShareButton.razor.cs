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

        [Parameter]
        public string EntryId { get; set; }

        [Parameter]
        public string StoryId { get; set; }

        [Parameter]
        public string BeingId { get; set; }

        [Parameter]
        public string ProfileId { get; set; }

        protected bool AreItemsLoading { get; set; }
        protected Modal Modal { get; set; }
        protected List<ShareModel> Items { get; set; }
        protected bool HasPublic { get; set; }

        protected ShareModel NewShare { get; } = new ShareModel();

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (!String.IsNullOrEmpty(EntryId))
                api = new EntryApi(Api, EntryId);
            else if (!String.IsNullOrEmpty(StoryId))
                api = new StoryApi(Api, StoryId);
            else if (!String.IsNullOrEmpty(BeingId))
                api = new BeingApi(Api, BeingId);
            else if (!String.IsNullOrEmpty(ProfileId))
                api = new ProfileApi(Api, ProfileId);
            else
                throw Ensure.Exception.InvalidOperation("One of 'entryId' and 'storyId' must be provided.");
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

        protected async Task OnPublicShareAsync() 
        {
            if (HasPublic)
                return;

            var share = new ShareModel(null, Permission.Read);
            await api.CreateAsync(share);
            await LoadAsync();
        }

        protected async Task OnAddAsync()
        {
            if (String.IsNullOrEmpty(NewShare.UserName) || String.IsNullOrWhiteSpace(NewShare.UserName))
                return;

            NewShare.UserName = NewShare.UserName.Trim();

            await api.CreateAsync(NewShare);
            await LoadAsync();

            NewShare.UserName = null;
            NewShare.Permission = Permission.Read;
        }

        protected async Task OnDeleteAsync(ShareModel model)
        {
            await api.DeleteAsync(model);
            await LoadAsync();
        }

        interface IApi
        {
            Task<List<ShareModel>> GetListAsync();
            Task CreateAsync(ShareModel model);
            Task DeleteAsync(ShareModel model);
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

            public Task CreateAsync(ShareModel model)
                => api.CreateEntryAsync(entryId, model);

            public Task DeleteAsync(ShareModel model)
                => api.DeleteEntryAsync(entryId, model);

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

            public Task CreateAsync(ShareModel model)
                => api.CreateStoryAsync(storyId, model);

            public Task DeleteAsync(ShareModel model)
                => api.DeleteStoryAsync(storyId, model);

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

            public Task CreateAsync(ShareModel model)
                => api.CreateBeingAsync(beingId, model);

            public Task DeleteAsync(ShareModel model)
                => api.DeleteBeingAsync(beingId, model);

            public Task<List<ShareModel>> GetListAsync()
                => api.GetBeingListAsync(beingId);
        }

        class ProfileApi : IApi
        {
            private readonly Api api;
            private readonly string profileId;

            public ProfileApi(Api api, string profileId)
            {
                Ensure.NotNull(api, "api");
                Ensure.NotNull(profileId, "profileId");
                this.api = api;
                this.profileId = profileId;
            }

            public Task CreateAsync(ShareModel model)
                => api.CreateProfileAsync(profileId, model);

            public Task DeleteAsync(ShareModel model)
                => api.DeleteProfileAsync(profileId, model);

            public Task<List<ShareModel>> GetListAsync()
                => api.GetProfileListAsync(profileId);
        }
    }
}
