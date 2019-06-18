using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollection.Accounts
{
    public class Interop
    {
        private readonly IJSRuntime jsRuntime;

        public Interop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public void SaveToken(string token)
            => jsRuntime.InvokeAsync<bool>("Recollection.SaveToken", token);

        public Task<string> LoadTokenAsync()
            => jsRuntime.InvokeAsync<string>("Recollection.LoadToken");
    }
}
