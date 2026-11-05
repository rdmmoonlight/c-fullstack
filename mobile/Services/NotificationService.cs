using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AumoFinance.Platforms.Android;

namespace AumoFinance.Services;

/// <summary>
/// Native Android implementation of the daily reminder notification.
/// Replaces the previous Plugin.LocalNotification-based implementation, which
/// could not be reliably resolved for this project's build configuration.
/// Uses AlarmManager + a BroadcastReceiver (see
/// Platforms/Android/ReminderBroadcastReceiver.cs) so the reminder still fires
/// even if the app itself isn't running.
/// App locked to Android 9 (API 28) only — no runtime notification
/// permission needed (that's an API 33+ requirement) and exact alarms are
/// always permitted (the CanScheduleExactAlarms restriction is API 31+).
/// </summary>
public class NotificationService
{
    public const string ChannelId = "aumo_daily_reminder";
    internal const int ReminderRequestCode = 2001;

    public Task<bool> RequestPermissionAsync()
    {
        // POST_NOTIFICATIONS is only required from Android 13 (API 33)
        // onward. This app's maxSdkVersion is 28, so it can never run on
        // a device where that permission is enforced.
        return Task.FromResult(true);
    }

    public Task ScheduleDailyReminderAsync(int hour = 20, int minute = 0)
    {
        EnsureChannel();

        var context = Android.App.Application.Context;

        var now = Java.Util.Calendar.Instance!;
        var trigger = Java.Util.Calendar.Instance!;
        trigger.Set(Java.Util.CalendarField.HourOfDay, hour);
        trigger.Set(Java.Util.CalendarField.Minute, minute);
        trigger.Set(Java.Util.CalendarField.Second, 0);
        trigger.Set(Java.Util.CalendarField.Millisecond, 0);

        if (trigger.TimeInMillis <= now.TimeInMillis)
        {
            trigger.Add(Java.Util.CalendarField.DayOfMonth, 1);
        }

        var pendingIntent = BuildReminderPendingIntent(context);
        var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService)!;

        // Exact alarms are unrestricted below Android 12 (API 31), and this
        // app never runs above API 28, so no CanScheduleExactAlarms() check
        // is needed here.
        alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, trigger.TimeInMillis, pendingIntent);

        return Task.CompletedTask;
    }

    public void CancelDailyReminder()
    {
        var context = Android.App.Application.Context;
        var pendingIntent = BuildReminderPendingIntent(context);

        var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService)!;
        alarmManager.Cancel(pendingIntent);
        pendingIntent.Cancel();
    }

    internal static PendingIntent BuildReminderPendingIntent(Context context)
    {
        var intent = new Intent(context, typeof(ReminderBroadcastReceiver));
        intent.SetAction(ReminderBroadcastReceiver.ActionShowReminder);

        return PendingIntent.GetBroadcast(
            context,
            ReminderRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    internal static void EnsureChannel()
    {
        // Notification channels exist since API 26; this app's minSdkVersion
        // is 28, so they are always available — no version check needed.
        var context = Android.App.Application.Context;
        var channel = new NotificationChannel(
            ChannelId,
            "Pengingat Harian",
            NotificationImportance.Default)
        {
            Description = "Pengingat harian untuk mencatat transaksi keuangan AumoFinance"
        };

        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        manager.CreateNotificationChannel(channel);
    }
}
