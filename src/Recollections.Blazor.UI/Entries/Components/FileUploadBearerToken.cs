using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using Neptuo.Recollections.Components;

namespace Neptuo.Recollections.Entries.Components;

public class FileUploadBearerToken(FileUploader fileUploader) : UserStateComponentBase, IDisposable
{
    protected async override Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        UserState.UserChanged += OnUserChanged;
        if (UserState.IsAuthenticated)
            await fileUploader.SetBearerTokenAsync(UserState.BearerToken);
    }

    public void Dispose()
    {
        UserState.UserChanged -= OnUserChanged;
    }

    private void OnUserChanged()
    {
        _ = fileUploader.SetBearerTokenAsync(UserState.BearerToken);
    }
}