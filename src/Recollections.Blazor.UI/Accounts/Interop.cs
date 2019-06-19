using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
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
            => jsRuntime.InvokeAsync<bool>("Recollections.SaveToken", token);

        public Task<string> LoadTokenAsync()
            => jsRuntime.InvokeAsync<string>("Recollections.LoadToken");
    }
}
