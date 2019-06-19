using Microsoft.AspNetCore.Components;
using Neptuo.Identifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class FileUploadModel : ComponentBase
    {
        [Inject]
        protected FileUploadInterop Interop { get; set; }

        [Inject]
        protected IUniqueNameProvider NameProvider { get; set; }

        [Parameter]
        protected string Url { get; set; }

        [Parameter]
        protected string BearerToken { get; set; }

        protected string FormId {get;private set; }

        protected override void OnInit()
        {
            base.OnInit();
            FormId = NameProvider.Next();
        }

        protected async override Task OnAfterRenderAsync()
        {
            await base.OnAfterRenderAsync();
            await Interop.InitializeAsync(FormId, BearerToken);
        }
    }
}
