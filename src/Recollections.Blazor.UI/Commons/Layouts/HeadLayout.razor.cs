using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
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

        [CascadingParameter]
        protected UserState UserState { get; set; }

        protected List<MenuItem> MenuItems { get; } = new List<MenuItem>();
        protected ExceptionPanel ExceptionPanel { get; set; }
        protected bool IsMainMenuVisible { get; set; } = false;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            MenuItems.Add(new MenuItem("Timeline", "stream", Navigator.UrlTimeline(), NavLinkMatch.All));
            MenuItems.Add(new MenuItem("Map", "map-marked-alt", Navigator.UrlMap()));
            MenuItems.Add(new MenuItem("Calendar", "calendar-alt", Navigator.UrlCalendar()));
            MenuItems.Add(new MenuItem("Search", "search", Navigator.UrlSearch()));
            MenuItems.Add(new MenuItem("Stories", "book", Navigator.UrlStories()));
            MenuItems.Add(new MenuItem("Beings", "user-friends", Navigator.UrlBeings()));
            MenuItems.Add(new MenuItem("About", "info-circle", Navigator.UrlAbout(), isSmall: false, isNewWindow: true));

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
        {
            UpdateMainMenuVisible(false);
            ExceptionPanel.Hide();
        }

        protected void ToggleMainMenu()
            => UpdateMainMenuVisible(!IsMainMenuVisible);

        protected async Task OnReadOnlyClickAsync()
        {
            await UserState.LogoutAsync();
            Navigator.OpenRegister();
        }
    }

    public class MenuItem
    {
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public NavLinkMatch Match { get; set; }

        public bool IsSmall { get; set; }
        public bool IsLarge { get; set; }

        public bool IsNewWindow { get; set; }

        public MenuItem(string text, string icon, string url, NavLinkMatch match = NavLinkMatch.Prefix, bool isSmall = true, bool isLarge = true, bool isNewWindow = false)
        {
            Text = text;
            Icon = icon;
            Url = url;
            Match = match;
            IsSmall = isSmall;
            IsLarge = isLarge;
            IsNewWindow = isNewWindow;
        }
    }
}
