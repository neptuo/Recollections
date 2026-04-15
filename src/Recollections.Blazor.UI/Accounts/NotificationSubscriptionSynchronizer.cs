using System;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts;

public class NotificationSubscriptionSynchronizer
{
    private readonly Api api;
    private readonly PushNotificationInterop pushInterop;

    public NotificationSubscriptionSynchronizer(Api api, PushNotificationInterop pushInterop)
    {
        Ensure.NotNull(api, "api");
        Ensure.NotNull(pushInterop, "pushInterop");
        this.api = api;
        this.pushInterop = pushInterop;
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

        bool wasRestored = false;
        if (isPushSupported && browserPermission == "granted" && !String.IsNullOrWhiteSpace(model.PushPublicKey))
        {
            if (currentBrowserSubscription == null && allowAutoRestore && model.HasSubscription)
            {
                currentBrowserSubscription = await pushInterop.SubscribeAsync(model.PushPublicKey);
                wasRestored = currentBrowserSubscription != null;
            }

            if (currentBrowserSubscription != null)
            {
                await api.CreateNotificationSubscriptionAsync(currentBrowserSubscription);
                model.HasSubscription = true;
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
        UserNotificationSettingsModel model = await api.GetNotificationSettingsAsync();
        await RefreshAsync(model);
    }
}

public record NotificationSubscriptionState(
    bool IsPushSupported,
    string BrowserPermission,
    PushSubscriptionModel CurrentBrowserSubscription,
    bool HasBrowserSubscription,
    bool WasRestored
);
