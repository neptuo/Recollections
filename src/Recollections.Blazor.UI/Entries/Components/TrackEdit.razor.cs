using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System.Globalization;

namespace Neptuo.Recollections.Entries.Components;

partial class TrackEdit
{
    protected Modal Modal { get; set; }
    protected string TotalElevationText => Track?.TotalElevation == null
        ? null
        : $"{Track.TotalElevation.Value.ToString("0.#", CultureInfo.InvariantCulture)} m";

    [Parameter]
    public EntryTrackModel Track { get; set; }

    [Parameter]
    public bool IsEditable { get; set; }

    [Parameter]
    public EventCallback Delete { get; set; }

    public void Show()
    {
        StateHasChanged();
        Modal.Show();
    }

    public void Hide()
        => Modal.Hide();
}
