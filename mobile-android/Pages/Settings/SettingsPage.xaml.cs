using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using AumoFinance.Services;

namespace AumoFinance.Pages.Settings;

public partial class SettingsPage : ContentPage
{
    private const string KeyReminderEnabled = "reminder_enabled";
    private const string KeyReminderHour = "reminder_hour";
    private const string KeyReminderMinute = "reminder_minute";
    private const string KeyAutoUpdateEnabled = "auto_update_enabled";

    private readonly NotificationService _notificationService;
    private readonly PeriodService _periodService;

    public SettingsPage(NotificationService notificationService, PeriodService periodService)
    {
        InitializeComponent();
        _notificationService = notificationService;
        _periodService = periodService;
        LoadSavedSettings();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (TopHeader != null)
        {
            await SelectedPeriodDisplayHelper.ApplyToTopBarAsync(TopHeader, _periodService);
        }
    }

    private void LoadSavedSettings()
    {
        bool isEnabled = Preferences.Default.Get(KeyReminderEnabled, true);
        int hour = Preferences.Default.Get(KeyReminderHour, 20); // Default 8:00 PM
        int minute = Preferences.Default.Get(KeyReminderMinute, 0);

        ReminderSwitch.IsToggled = isEnabled;
        ReminderTimePicker.Time = new TimeSpan(hour, minute, 0);
        TimePickerContainer.IsVisible = isEnabled;

        // Load status Auto Update
        AutoUpdateSwitch.IsToggled = Preferences.Default.Get(KeyAutoUpdateEnabled, true);
    }

    private void OnReminderToggled(object? sender, ToggledEventArgs e)
    {
        TimePickerContainer.IsVisible = e.Value;
    }

    private void OnTimeSelected(object? sender, TimeChangedEventArgs e)
    {
        // Handled upon clicking Save
    }

    private void OnAutoUpdateToggled(object? sender, ToggledEventArgs e)
    {
        Preferences.Default.Set(KeyAutoUpdateEnabled, e.Value);
    }

    private async void OnCheckUpdateManualClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement button)
        {
            button.IsEnabled = false;
        }

        try
        {
            // Tambahkan logika pengecekan update manual jika ada
            await DisplayAlertAsync("Check Update", "App is up to date.", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnCheckUpdateManualClicked error: {ex}");
            await DisplayAlertAsync("Error", "Failed to check for updates.", "OK");
        }
        finally
        {
            if (sender is VisualElement btn)
            {
                btn.IsEnabled = true;
            }
        }
    }

    private async void OnSaveSettingsClicked(object? sender, EventArgs e)
    {
        bool isEnabled = ReminderSwitch.IsToggled;

        // ReminderTimePicker.Time bertipe TimeSpan? di versi MAUI ini — fallback ke jam 00:00 jika belum dipilih
        TimeSpan selectedTime = ReminderTimePicker.Time ?? TimeSpan.Zero;

        Preferences.Default.Set(KeyReminderEnabled, isEnabled);
        Preferences.Default.Set(KeyReminderHour, selectedTime.Hours);
        Preferences.Default.Set(KeyReminderMinute, selectedTime.Minutes);

        if (isEnabled)
        {
            await _notificationService.ScheduleDailyReminderAsync(selectedTime.Hours, selectedTime.Minutes);
            await DisplayAlertAsync("Settings Saved", $"Daily reminder set for {DateTime.Today.Add(selectedTime):hh:mm tt}.", "OK");
        }
        else
        {
            _notificationService.CancelDailyReminder();
            await DisplayAlertAsync("Settings Saved", "Daily reminder has been disabled.", "OK");
        }
    }
}
