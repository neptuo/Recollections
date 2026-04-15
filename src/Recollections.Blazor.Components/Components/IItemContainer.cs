using System;
using Microsoft.AspNetCore.Components;

namespace Neptuo.Recollections.Components;

public interface IItemContainer
{
    void Close();
    RenderFragment<Item> GetItemTemplate();
    RenderFragment<ItemGroup> GetItemGroupTemplate();
    RenderFragment<ItemSeparator> GetItemSeparatorTemplate();
}
