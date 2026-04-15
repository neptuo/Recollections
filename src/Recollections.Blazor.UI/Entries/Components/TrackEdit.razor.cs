using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;
using System.Globalization;

namespace Neptuo.Recollections.Entries.Components;

partial class TrackEdit
{
    protected Modal Modal { get; set; }
    protected string TotalDistanceText => Track?.TotalDistance == null
        ? null
        : Track.TotalDistance.Value >= 1000d
            ? $"{(Track.TotalDistance.Value / 1000d).ToString("0.##", CultureInfo.InvariantCulture)} km"
            : $"{Track.TotalDistance.Value.ToString("0.#", CultureInfo.InvariantCulture)} m";
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
