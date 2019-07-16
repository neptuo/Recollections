﻿using Microsoft.AspNetCore.Components;
using Neptuo.Identifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class FileUploadModel : ComponentBase, IDisposable
    {
        public const string DefaultText = "Upload Images";

        [Inject]
        protected FileUploadInterop Interop { get; set; }

        [Inject]
        protected IUniqueNameProvider NameProvider { get; set; }

        [Parameter]
        protected string Text { get; set; } = DefaultText;

        [Parameter]
        protected string Url { get; set; }

        [Parameter]
        protected string BearerToken { get; set; }

        [Parameter]
        protected Action<FileUploadProgress> Progress { get; set; }

        [Parameter]
        protected Action<FileUploadProgress> Error { get; set; }

        public string FormId { get; private set; }

        protected override void OnInit()
        {
            base.OnInit();
            FormId = NameProvider.Next();
        }

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();
            await Interop.InitializeAsync(this, BearerToken);
        }

        internal void OnCompleted(int total, int completed)
        {
            Console.WriteLine("FileUploadModel.OnCompleted");
            Progress?.Invoke(new FileUploadProgress(total, completed));
        }

        internal void OnError(int total, int completed)
        {
            Console.WriteLine("FileUploadModel.OnError");
            Error?.Invoke(new FileUploadProgress(total, completed));
        }

        public void Dispose()
        {
            Interop.Destroy(this);
        }
    }
}
