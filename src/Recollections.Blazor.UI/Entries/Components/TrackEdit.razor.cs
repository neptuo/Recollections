using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;

namespace Neptuo.Recollections.Entries.Components;

partial class TrackEdit
{
    protected Modal Modal { get; set; }

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
