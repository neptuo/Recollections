using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Neptuo.Recollections.Accounts.Components;

public abstract class UserStateComponentBase : ComponentBase
{
    [CascadingParameter]
    protected UserState UserState { get; set; }
}

public abstract class UserStateAuthenticatedComponentBase : UserStateComponentBase
{
}