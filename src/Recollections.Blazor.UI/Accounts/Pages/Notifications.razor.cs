using Microsoft.AspNetCore.Components;
using Neptuo.Recollections.Accounts.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts.Pages;

public partial class Notifications
{
    [Inject]
    protected Api Api { get; set; }

    [Inject]
    protected PushNotificationInterop PushInterop { get; set; }

    [Inject]
    protected NotificationSubscriptionSynchronizer NotificationSynchronizer { get; set; }

    protected List<string> ErrorMessages { get; } = new();

    protected UserNotificationSettingsModel Model { get; set; } = new();
    protected PushSubscriptionModel CurrentBrowserSubscription { get; set; }

    protected bool IsLoading { get; set; } = true;
    protected bool IsBusy { get; set; }
    protected bool IsPushSupported { get; set; }
    protected bool HasBrowserSubscription { get; set; }
    protected string BrowserPermission { get; set; } = "default";
    protected string StatusMessage { get; set; }

    protected bool CanSubscribe
        => !IsBusy
            && IsPushSupported
            && !HasBrowserSubscription
            && !String.IsNullOrWhiteSpace(Model?.PushPublicKey);

    protected string BrowserSubscriptionButtonCssClass
        => HasBrowserSubscription ? "btn btn-secondary" : "btn btn-primary";

    protected string BrowserSubscriptionButtonIcon
        => HasBrowserSubscription ? "bell-slash" : "bell";

    protected string BrowserSubscriptionButtonText
        => HasBrowserSubscription ? "Disable on this browser" : "Enable on this browser";

    protected string BrowserPermissionLabel => BrowserPermission switch
    {
        "granted" => "Granted",
        "denied" => "Denied",
        "unsupported" => "Unsupported",
        _ => "Not requested"
    };

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
        await EnsureTimeZoneAsync();
    }

    private async Task EnsureTimeZoneAsync()
    {
        if (Model?.OnThisDay == null)
            return;

        if (!String.IsNullOrWhiteSpace(Model.OnThisDay.TimeZone) && Model.OnThisDay.TimeZone != "UTC")
            return;

        try
        {
            string detected = await PushInterop.GetTimeZoneAsync();
            if (!String.IsNullOrWhiteSpace(detected))
                Model.OnThisDay.TimeZone = detected;
        }
        catch
        {
            // Ignore detection failures; user can keep the stored value.
        }
    }

    protected async Task SaveAsync()
    {
        await RunBusyAsync(async () =>
        {
            await Api.SetNotificationSettingsAsync(Model);
            Model = await Api.GetNotificationSettingsAsync();
            StatusMessage = "Notification settings saved.";
            await RefreshBrowserStateAsync();
        });
    }

    protected async Task SubscribeAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (String.IsNullOrWhiteSpace(Model.PushPublicKey))
                throw new InvalidOperationException("Push notifications are not configured on the server yet.");

            Model.IsEnabled = true;
            Model.NewEntries.IsEnabled = true;
            Model.OnThisDay.IsEnabled = true;
            await EnsureTimeZoneAsync();
            await Api.SetNotificationSettingsAsync(Model);

            CurrentBrowserSubscription = await PushInterop.SubscribeAsync(Model.PushPublicKey);
            await Api.CreateNotificationSubscriptionAsync(CurrentBrowserSubscription);
            await NotificationSynchronizer.MarkEnabledAsync(CurrentBrowserSubscription);

            StatusMessage = "Push notifications are enabled on this browser.";
            await LoadAsync();
        });
    }

    protected async Task UnsubscribeAsync()
    {
        await RunBusyAsync(async () =>
        {
            CurrentBrowserSubscription ??= await PushInterop.GetSubscriptionAsync();
            if (CurrentBrowserSubscription != null)
            {
                await Api.DeleteNotificationSubscriptionAsync(CurrentBrowserSubscription.Endpoint);
                await PushInterop.UnsubscribeAsync();
            }

            await NotificationSynchronizer.MarkDisabledAsync();

            StatusMessage = "Push notifications are disabled on this browser.";
            await LoadAsync();
        });
    }

    protected Task ToggleSubscriptionAsync()
        => HasBrowserSubscription ? UnsubscribeAsync() : SubscribeAsync();

    private async Task LoadAsync()
    {
        ErrorMessages.Clear();
        IsLoading = true;

        Model = await Api.GetNotificationSettingsAsync();
        Model.OnThisDay ??= new UserNotificationOnThisDaySettingsModel();
        await RefreshBrowserStateAsync();

        IsLoading = false;
    }

    private async Task RefreshBrowserStateAsync()
    {
        NotificationSubscriptionState state = await NotificationSynchronizer.RefreshAsync(Model);
        IsPushSupported = state.IsPushSupported;
        BrowserPermission = state.BrowserPermission;
        CurrentBrowserSubscription = state.CurrentBrowserSubscription;
        HasBrowserSubscription = state.HasBrowserSubscription;

        if (state.WasRestored && String.IsNullOrEmpty(StatusMessage))
            StatusMessage = "Push notifications were restored on this browser.";
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        ErrorMessages.Clear();
        IsBusy = true;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessages.Add(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
