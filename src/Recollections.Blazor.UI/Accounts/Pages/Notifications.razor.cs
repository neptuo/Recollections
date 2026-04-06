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
    }

    protected async Task SaveAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (String.IsNullOrWhiteSpace(Model.TimeZoneId))
                Model.TimeZoneId = await PushInterop.GetTimeZoneAsync();

            await Api.SetNotificationSettingsAsync(Model);
            Model = await Api.GetNotificationSettingsAsync();
            StatusMessage = "Notification settings saved.";
            await RefreshBrowserStateAsync();
        });
    }

    protected Task UseBrowserTimeZoneAsync()
        => RunBusyAsync(async () => Model.TimeZoneId = await PushInterop.GetTimeZoneAsync());

    protected async Task SubscribeAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (String.IsNullOrWhiteSpace(Model.PushPublicKey))
                throw new InvalidOperationException("Push notifications are not configured on the server yet.");

            if (String.IsNullOrWhiteSpace(Model.TimeZoneId))
                Model.TimeZoneId = await PushInterop.GetTimeZoneAsync();

            Model.IsEnabled = true;
            Model.NewEntries.IsEnabled = true;
            await Api.SetNotificationSettingsAsync(Model);

            CurrentBrowserSubscription = await PushInterop.SubscribeAsync(Model.PushPublicKey);
            await Api.CreateNotificationSubscriptionAsync(CurrentBrowserSubscription);

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
                await Api.DeleteNotificationSubscriptionAsync(CurrentBrowserSubscription);
                await PushInterop.UnsubscribeAsync();
            }

            StatusMessage = "Push notifications are disabled on this browser.";
            await LoadAsync();
        });
    }

    private async Task LoadAsync()
    {
        ErrorMessages.Clear();
        IsLoading = true;

        Model = await Api.GetNotificationSettingsAsync();
        await RefreshBrowserStateAsync();

        if (String.IsNullOrWhiteSpace(Model.TimeZoneId))
            Model.TimeZoneId = await PushInterop.GetTimeZoneAsync();

        IsLoading = false;
    }

    private async Task RefreshBrowserStateAsync()
    {
        IsPushSupported = await PushInterop.IsSupportedAsync();
        BrowserPermission = IsPushSupported
            ? await PushInterop.GetPermissionAsync()
            : "unsupported";

        CurrentBrowserSubscription = IsPushSupported
            ? await PushInterop.GetSubscriptionAsync()
            : null;

        HasBrowserSubscription = CurrentBrowserSubscription != null;
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
