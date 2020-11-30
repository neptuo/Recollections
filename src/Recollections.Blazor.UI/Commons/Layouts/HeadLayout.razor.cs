using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Layouts
{
    public partial class HeadLayout : IDisposable
    {
        [Inject]
        internal Navigator Navigator { get; set; }

        protected bool IsMainMenuVisible { get; set; } = false;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Navigator.LocationChanged += OnLocationChanged;
        }

        public void Dispose()
        {
            Navigator.LocationChanged -= OnLocationChanged;
        }

        private void UpdateMainMenuVisible(bool isVisible)
        {
            if (IsMainMenuVisible != isVisible)
            {
                IsMainMenuVisible = isVisible;
                StateHasChanged();
            }
        }

        private void OnLocationChanged(string url)
            => UpdateMainMenuVisible(false);

        protected void ToggleMainMenu()
            => UpdateMainMenuVisible(!IsMainMenuVisible);
    }
}
