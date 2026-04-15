using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Pages;

public partial class HighestAltitude
{
    [Inject]
    protected Navigator Navigator { get; set; }

    [Inject]
    protected Api Api { get; set; }

    [Inject]
    protected UiOptions UiOptions { get; set; }

    protected List<EntryListModel> Items { get; } = [];
    protected bool IsLoading { get; set; }

    protected string FormatEntryTitle(EntryListModel entry)
        => entry.Altitude != null
            ? $"{UiOptions.FormatWholeNumber(entry.Altitude.Value)} m"
            : entry.When.ToString(UiOptions.ShortDateFormat);

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        try
        {
            IsLoading = true;
            var entries = await Api.GetHighestAltitudeListAsync();
            Items.AddRange(entries);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
