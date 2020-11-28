using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class WindowInterop
    {
        private readonly IJSInProcessRuntime jsRuntime;

        public WindowInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = (IJSInProcessRuntime)jsRuntime;
        }

        public void ScrollTo(int x, int y) => jsRuntime.InvokeVoid("window.scrollTo", x, y);
    }
}
