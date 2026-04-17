using Microsoft.JSInterop;
using Neptuo;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public class PushNotificationInterop
    {
        private readonly IJSRuntime jsRuntime;

        public PushNotificationInterop(IJSRuntime jsRuntime)
        {
            Ensure.NotNull(jsRuntime, "jsRuntime");
            this.jsRuntime = jsRuntime;
        }

        public ValueTask<bool> IsSupportedAsync()
            => jsRuntime.InvokeAsync<bool>("Recollections.Notifications.isSupported");

        public ValueTask<string> GetPermissionAsync()
            => jsRuntime.InvokeAsync<string>("Recollections.Notifications.getPermission");

        public ValueTask<string> GetTimeZoneAsync()
            => jsRuntime.InvokeAsync<string>("Recollections.Notifications.getTimeZone");

        public ValueTask<PushSubscriptionModel> GetSubscriptionAsync()
            => jsRuntime.InvokeAsync<PushSubscriptionModel>("Recollections.Notifications.getSubscription");

        public ValueTask<PushSubscriptionModel> SubscribeAsync(string publicKey)
            => jsRuntime.InvokeAsync<PushSubscriptionModel>("Recollections.Notifications.subscribe", publicKey);

        public ValueTask<PushSubscriptionModel> UnsubscribeAsync()
            => jsRuntime.InvokeAsync<PushSubscriptionModel>("Recollections.Notifications.unsubscribe");
    }
}
