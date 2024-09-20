using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Commons.Components;
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

        [Parameter]
        public ButtonLayout Layout { get; set; }

        [CascadingParameter]
        public UserState UserState { get; set; }

        protected bool IsLoading { get; set; }
        protected Modal Modal { get; set; }
        protected ShareRootModel Model { get; set; }
        protected bool HasPublic { get; set; }

        protected string ErrorMessage { get; set; }
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
            IsLoading = true;
            Model = await api.GetAsync();
            HasPublic = Model.Models.Any(s => s.UserName == "public");
            IsLoading = false;

            StateHasChanged();
        }

        protected void OnShow()
        {
            ErrorMessage = null;
            Modal.Show();
            _ = LoadAsync();
        }

        protected async Task SaveAsync() 
        {
            ErrorMessage = null;
            if (await api.SaveAsync(Model)) 
            {
                Modal.Hide();
            }
            else
            {
                ErrorMessage = "Saving failed. ";
                if (EntryId != null)
                    ErrorMessage += "Check that story and beings owners still has co-owner access.";
                else if (StoryId != null)
                    ErrorMessage += "Check that entry owners still has co-owner access.";
                else if (BeingId != null)
                    ErrorMessage += "Check that entry owners still has at least reader access.";

                ErrorMessage += " If so, try again later please.";
            }
        }

        interface IApi
        {
            Task<ShareRootModel> GetAsync();
            Task<bool> SaveAsync(ShareRootModel model);
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

            public Task<bool> SaveAsync(ShareRootModel model)
                => api.SaveEntryAsync(entryId, model);

            public Task<ShareRootModel> GetAsync()
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

            public Task<bool> SaveAsync(ShareRootModel model)
                => api.SaveStoryAsync(storyId, model);

            public Task<ShareRootModel> GetAsync()
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

            public Task<bool> SaveAsync(ShareRootModel model)
                => api.SaveBeingAsync(beingId, model);

            public Task<ShareRootModel> GetAsync()
                => api.GetBeingListAsync(beingId);
        }
    }
}
