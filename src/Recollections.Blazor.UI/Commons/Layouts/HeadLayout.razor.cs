using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;
using Neptuo.Recollections.Entries.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Layouts;

public partial class HeadLayout : IDisposable
{
    [Inject]
    internal Navigator Navigator { get; set; }

    [CascadingParameter]
    protected UserState UserState { get; set; }

    protected MenuList Menu { get; private set; }
    protected ExceptionPanel ExceptionPanel { get; set; }
    protected ChangePasswordModal ChangePasswordModal { get; set; }
    protected bool IsMainMenuVisible { get; set; } = false;

    protected override void OnInitialized()
    {
        Menu = new MenuList(Navigator, UserState, OnChangePassword);
        base.OnInitialized();

        Navigator.LocationChanged += OnLocationChanged;
    }

    public void Dispose()
    {
        Navigator.LocationChanged -= OnLocationChanged;
    }

    protected void OnChangePassword() => ChangePasswordModal.Show();

    private void OnLocationChanged(string url)
    {
        ExceptionPanel.Hide();
    }

    protected async Task OnReadOnlyClickAsync()
    {
        await UserState.LogoutAsync();
        Navigator.OpenRegister();
    }
}

public class MenuList
{
    public List<MenuItem> Bottom { get; } = new();
    public List<MenuItem> Main { get; } = new();
    public List<MenuItem> User { get; } = new();

    public MenuList(Navigator navigator, UserState userState, Action onChangePassword)
    {
        Add(new MenuItem("Main menu", "bars"), Bottom);
        Add(new MenuItem("Timeline", "stream", Url: navigator.UrlTimeline(), PageType: typeof(TimelineList), Match: NavLinkMatch.All), Main, Bottom);
        Add(new MenuItem("Map", "map-marked-alt", Url: navigator.UrlMap()), Main);
        Add(new MenuItem("Calendar", "calendar-alt", Url: navigator.UrlCalendar()), Main);
        Add(new MenuItem("Search", "search", Url: navigator.UrlSearch()), Main, Bottom);
        Add(new MenuItem("Stories", "book", Url: navigator.UrlStories()), Main, Bottom);
        Add(new MenuItem("Beings", "user-friends", Url: navigator.UrlBeings()), Main);
        Add(new MenuItem("About", "info-circle", Url: navigator.UrlAbout(), IsNewWindow: true), Main);

        Add(new MenuItem("Profile", "address-card", OnClick: () => navigator.OpenProfile(userState.UserId)), User);
        Add(new MenuItem("Connections", "link", Url: navigator.UrlConnections()), User);
        Add(new MenuItem("Change password", "key", OnClick: onChangePassword), User);
        Add(new MenuItem("Logout", "sign-out-alt", OnClick: () => _ = userState.LogoutAsync()), User);
    }

    private void Add(MenuItem item, params List<MenuItem>[] groups)
    {
        foreach (var group in groups)
            group.Add(item);
    }
}

public record MenuItem(string Text, string Icon, string Url = null, Type PageType = null, Action OnClick = null, NavLinkMatch Match = NavLinkMatch.Prefix, bool IsNewWindow = false);
