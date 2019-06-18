using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class ModalNative
    {
        private readonly IJSRuntime jsRuntime;
        private Dictionary<string, ModalModel> modals = new Dictionary<string, ModalModel>();

        public ModalNative(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        internal void AddModal(string id, ModalModel component)
        {
            modals[id] = component;
            jsRuntime.InvokeAsync<object>("Bootstrap.Modal.Register", id);
        }

        internal void ToggleModal(string id, bool isVisible)
            => jsRuntime.InvokeAsync<object>("Bootstrap.Modal.Toggle", id, isVisible);

        internal void RemoveModal(string id)
            => modals.Remove(id);

        [JSInvokable]
        public void Bootstrap_ModalHidden(string id)
        {
            Console.WriteLine($"Modal hidden '{id}'.");
            if (modals.TryGetValue(id, out ModalModel modal))
                modal.MarkAsHidden();
            else
                Console.WriteLine($"Modal not found '{id}'.");
        }
    }
}
