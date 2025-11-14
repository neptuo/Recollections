using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Neptuo.Recollections.Accounts.Components;

public abstract class UserStateComponentBase : ComponentBase
{
    [CascadingParameter]
    protected UserState UserState { get; set; }

    protected async override Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await UserState.EnsureInitializedAsync();
    }

    protected Task EnsureAuthenticatedAsync()
        => UserState.EnsureAuthenticatedAsync();
}