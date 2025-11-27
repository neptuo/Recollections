using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Routing;

namespace Neptuo.Recollections.Components;

public record MenuItem(
    string Text, 
    string Icon, 
    string Url = null, 
    Type PageType = null, 
    Action OnClick = null, 
    NavLinkMatch Match = NavLinkMatch.Prefix, 
    bool IsNewWindow = false,
    string CssClass = ""
);

public record MenuItemGroup(string Text, List<MenuItem> Items);
