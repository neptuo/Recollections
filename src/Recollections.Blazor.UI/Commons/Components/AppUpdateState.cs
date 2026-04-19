using Blazored.LocalStorage;
using Neptuo;
using System;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Commons.Components;

public class AppUpdateState
{
    private const string KeyPrefix = "app-update";
    private const string ClientVersionKey = "app-update:client-version";
    private readonly ILocalStorageService localStorage;
    private readonly PwaInstallInterop pwaInstallInterop;

    public AppUpdateState(ILocalStorageService localStorage, PwaInstallInterop pwaInstallInterop)
    {
        Ensure.NotNull(localStorage, "localStorage");
        Ensure.NotNull(pwaInstallInterop, "pwaInstallInterop");
        this.localStorage = localStorage;
        this.pwaInstallInterop = pwaInstallInterop;
    }

    public async Task<string> GetCurrentVersionAsync()
    {
        string version = await pwaInstallInterop.GetVersionAsync();
        return String.IsNullOrWhiteSpace(version) ? "development" : version;
    }

    public async Task<bool> HasVersionChangedAsync(string scope)
    {
        Ensure.NotNullOrEmpty(scope, "scope");

        string current = await GetCurrentVersionAsync();
        string previous = await localStorage.GetItemAsync<string>($"{KeyPrefix}:{scope}:version");
        return !String.Equals(current, previous, StringComparison.Ordinal);
    }

    public async Task RememberCurrentVersionAsync(string scope)
    {
        Ensure.NotNullOrEmpty(scope, "scope");
        await localStorage.SetItemAsync($"{KeyPrefix}:{scope}:version", await GetCurrentVersionAsync());
    }

    public async Task ClearVersionAsync(string scope)
    {
        Ensure.NotNullOrEmpty(scope, "scope");
        await localStorage.RemoveItemAsync($"{KeyPrefix}:{scope}:version");
    }

    public string GetClientVersion()
        => typeof(AppUpdateState).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public async Task<string> GetLastSeenClientVersionAsync()
        => await localStorage.GetItemAsync<string>(ClientVersionKey);

    public async Task RememberClientVersionAsync()
        => await localStorage.SetItemAsync(ClientVersionKey, GetClientVersion());
}
