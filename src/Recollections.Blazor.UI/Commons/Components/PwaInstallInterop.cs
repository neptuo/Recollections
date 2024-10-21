using Microsoft.JSInterop;
using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Components
{
    public class PwaInstallInterop
    {
        public interface IComponent
        {
            void MakeInstallable();
            void MakeUpdateable();
        }

        private static bool isInstallable;
        private static bool isUpdateable;

        private static List<IComponent> editors = new List<IComponent>();
        private readonly IJSRuntime jSRuntime;

        public PwaInstallInterop(IJSRuntime jSRuntime)
        {
            Ensure.NotNull(jSRuntime, "jSRuntime");
            this.jSRuntime = jSRuntime;
        }

        public void Initialize(IComponent editor)
        {
            Ensure.NotNull(editor, "editor");
            editors.Add(editor);

            if (isInstallable)
                editor.MakeInstallable();
            else if (isUpdateable)
                editor.MakeUpdateable();
        }

        public void Remove(IComponent editor)
        {
            Ensure.NotNull(editor, "editor");
            editors.Remove(editor);
        }

        [JSInvokable("Pwa.Installable")]
        public static void Installable()
        {
            isInstallable = true;
            isUpdateable = false;

            foreach (var editor in editors.ToArray())
                editor.MakeInstallable();
        }

        [JSInvokable("Pwa.Updateable")]
        public static void Updateable()
        {
            isInstallable = false;
            isUpdateable = true;

            foreach (var editor in editors.ToArray())
                editor.MakeUpdateable();
        }

        public ValueTask InstallAsync() 
            => jSRuntime.InvokeVoidAsync("Pwa.Install");

        public ValueTask UpdateAsync() 
            => jSRuntime.InvokeVoidAsync("Pwa.Update");
    }
}
