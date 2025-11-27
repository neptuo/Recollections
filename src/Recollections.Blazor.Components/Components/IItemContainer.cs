using System;
using Microsoft.AspNetCore.Components;

namespace Neptuo.Recollections.Components;

public interface IItemContainer
{
    RenderFragment<Item> GetItemTemplate();
    RenderFragment<ItemGroup> GetItemGroupTemplate();
}