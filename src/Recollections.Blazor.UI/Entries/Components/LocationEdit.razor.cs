
using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Components;

namespace Neptuo.Recollections.Entries.Components;

partial class LocationEdit
{
    protected Modal Modal { get; set; }
    protected LocationModel SelectedLocation { get; set; }

    [Parameter]
    public EventCallback Save { get; set; }

    [Parameter]
    public EventCallback Delete { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
    }

    public void Show(LocationModel model)
    {
        SelectedLocation = model;
        Modal.Show();
    }

    public void Hide()
        => Modal.Hide();
}