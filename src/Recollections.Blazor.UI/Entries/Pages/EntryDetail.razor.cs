﻿using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages
{
    public class EntryDetailModel : ComponentBase
    {
        [Inject]
        protected Api Api { get; set; }

        [Inject]
        protected Navigator Navigator { get; set; }

        [Inject]
        protected UiOptions UiOptions { get; set; }

        [CascadingParameter]
        protected UserStateModel UserState { get; set; }

        [Parameter]
        protected string EntryId { get; set; }

        private EntryModel original;
        protected EntryModel Model { get; set; }
        protected List<ImageModel> Images { get; set; }

        protected async override Task OnInitAsync()
        {
            await base.OnInitAsync();
            await UserState.EnsureAuthenticatedAsync();

            Model = await Api.GetDetailAsync(EntryId);
            original = Model.Clone();

            await LoadImagesAsync();
        }

        private async Task LoadImagesAsync()
        {
            Images = await Api.GetImagesAsync(EntryId);
        }

        protected async Task SaveTitleAsync(string value)
        {
            Model.Title = value;
            await SaveAsync();
        }

        protected async Task SaveTextAsync(string value)
        {
            Model.Text = value;
            await SaveAsync();
        }

        protected async Task SaveWhenAsync(DateTime value)
        {
            Model.When = value;
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            Console.WriteLine("Model: " + SimpleJson.SimpleJson.SerializeObject(Model));
            Console.WriteLine("Original: " + SimpleJson.SimpleJson.SerializeObject(original));

            if (original.Equals(Model))
                return;

            await Api.UpdateAsync(Model);
            original = Model.Clone();
        }

        protected async Task OnUploadCompletedAsync()
        {
            await LoadImagesAsync();
            StateHasChanged();
        }

        public async Task DeleteAsync()
        {
            if (await Navigator.AskAsync($"Do you really want to delete entry '{Model.Title}'?"))
            {
                await Api.DeleteAsync(Model.Id);
                Navigator.OpenTimeline();
            }
        }
    }
}
