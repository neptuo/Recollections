using Blazored.LocalStorage;
using Neptuo;
using Neptuo.Recollections.Commons.Components;
using System;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts;

public class NotificationSubscriptionSynchronizer
{
    private const string SyncScope = "notifications-browser-subscription";
    private const string SyncStateKey = "notifications:browser-subscription";

    private readonly Api api;
    private readonly PushNotificationInterop pushInterop;
    private readonly ILocalStorageService localStorage;
    private readonly AppUpdateState appUpdateState;

    public NotificationSubscriptionSynchronizer(Api api, PushNotificationInterop pushInterop, ILocalStorageService localStorage, AppUpdateState appUpdateState)
    {
        Ensure.NotNull(api, "api");
        Ensure.NotNull(pushInterop, "pushInterop");
        Ensure.NotNull(localStorage, "localStorage");
        Ensure.NotNull(appUpdateState, "appUpdateState");
        this.api = api;
        this.pushInterop = pushInterop;
        this.localStorage = localStorage;
        this.appUpdateState = appUpdateState;
    }

    public async Task<NotificationSubscriptionState> RefreshAsync(UserNotificationSettingsModel model, bool allowAutoRestore = true)
    {
        Ensure.NotNull(model, "model");

        bool isPushSupported = await pushInterop.IsSupportedAsync();
        string browserPermission = isPushSupported
            ? await pushInterop.GetPermissionAsync()
            : "unsupported";

        PushSubscriptionModel currentBrowserSubscription = isPushSupported
            ? await pushInterop.GetSubscriptionAsync()
            : null;

        StoredSubscriptionSyncState syncState = await LoadSyncStateAsync();
        bool versionChanged = await appUpdateState.HasVersionChangedAsync(SyncScope);
        bool wasRestored = false;
        if (isPushSupported && browserPermission == "granted" && !String.IsNullOrWhiteSpace(model.PushPublicKey))
        {
            if (currentBrowserSubscription == null && allowAutoRestore && syncState.IsEnabled)
            {
                currentBrowserSubscription = await pushInterop.SubscribeAsync(model.PushPublicKey);
                wasRestored = currentBrowserSubscription != null;
            }

            if (currentBrowserSubscription != null && ShouldSyncSubscription(syncState, currentBrowserSubscription, model.HasSubscription, versionChanged, wasRestored))
            {
                await api.CreateNotificationSubscriptionAsync(currentBrowserSubscription);
                model.HasSubscription = true;
                await SaveSyncStateAsync(currentBrowserSubscription, true);
            }
            else if (currentBrowserSubscription != null)
            {
                model.HasSubscription = model.HasSubscription || syncState.IsEnabled;
            }
        }

        return new NotificationSubscriptionState(
            isPushSupported,
            browserPermission,
            currentBrowserSubscription,
            currentBrowserSubscription != null,
            wasRestored
        );
    }

    public async Task EnsureCurrentBrowserAsync()
    {
        if (!await pushInterop.IsSupportedAsync())
            return;

        string browserPermission = await pushInterop.GetPermissionAsync();
        PushSubscriptionModel currentBrowserSubscription = await pushInterop.GetSubscriptionAsync();
        StoredSubscriptionSyncState syncState = await LoadSyncStateAsync();
        if (browserPermission != "granted" && currentBrowserSubscription == null)
            return;

        if (currentBrowserSubscription == null && !syncState.IsEnabled)
            return;

        UserNotificationSettingsModel model = await api.GetNotificationSettingsAsync();
        await RefreshAsync(model);
    }

    public async Task MarkEnabledAsync(PushSubscriptionModel currentBrowserSubscription)
        => await SaveSyncStateAsync(currentBrowserSubscription, true);

    public async Task MarkDisabledAsync()
    {
        await localStorage.RemoveItemAsync(SyncStateKey);
        await appUpdateState.ClearVersionAsync(SyncScope);
    }

    private async Task SaveSyncStateAsync(PushSubscriptionModel currentBrowserSubscription, bool isEnabled)
    {
        await localStorage.SetItemAsync(
            SyncStateKey,
            new StoredSubscriptionSyncState(currentBrowserSubscription?.Endpoint, isEnabled)
        );
        await appUpdateState.RememberCurrentVersionAsync(SyncScope);
    }

    private async Task<StoredSubscriptionSyncState> LoadSyncStateAsync()
        => await localStorage.GetItemAsync<StoredSubscriptionSyncState>(SyncStateKey)
            ?? new StoredSubscriptionSyncState(null, false);

    private static bool ShouldSyncSubscription(StoredSubscriptionSyncState syncState, PushSubscriptionModel currentBrowserSubscription, bool hasServerSubscription, bool versionChanged, bool wasRestored)
    {
        if (wasRestored || versionChanged || !hasServerSubscription || !syncState.IsEnabled)
            return true;

        return !String.Equals(syncState.Endpoint, currentBrowserSubscription?.Endpoint, StringComparison.Ordinal);
    }
}

public record NotificationSubscriptionState(
    bool IsPushSupported,
    string BrowserPermission,
    PushSubscriptionModel CurrentBrowserSubscription,
    bool HasBrowserSubscription,
    bool WasRestored
);

public record StoredSubscriptionSyncState(
    string Endpoint,
    bool IsEnabled
);
