using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Entries.Beings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages;

public partial class Connections
{
    [Inject]
    protected Api Api { get; set; }

    [CascadingParameter]
    protected UserState UserState { get; set; }


    protected bool IsLoading { get; set; }

    public string UserName { get; set; }
    public List<string> ErrorMessages { get; } = new List<string>();

    public List<ConnectionModel> Items { get; } = new List<ConnectionModel>();

    protected async override Task OnInitializedAsync()
    {
        IsLoading = true;

        await base.OnInitializedAsync();
        await UserState.EnsureAuthenticatedAsync();

        await LoadDataAsync();
    }

    protected async Task LoadDataAsync()
    {
        IsLoading = true;
        Items.Clear();
        Items.AddRange(await Api.GetConnectionsAsync());
        IsLoading = false;
    }

    protected async Task CreateAsync()
    {
        var model = new ConnectionModel()
        {
            OtherUserName = UserName,
            Role = ConnectionRole.Initiator,
            State = ConnectionState.Pending
        };

        await Api.CreateConnectionAsync(model);
        UserName = null;

        await LoadDataAsync();
    }

    protected async Task ChangeStateAsync(ConnectionModel model, ConnectionState newState)
    {
        model.State = newState;
        await Api.UpdateConnectionAsync(model);
        await LoadDataAsync();
    }

    protected async Task DeleteAsync(ConnectionModel model)
    {
        await Api.DeleteConnectionAsync(model.OtherUserName);
        await LoadDataAsync();
    }
}