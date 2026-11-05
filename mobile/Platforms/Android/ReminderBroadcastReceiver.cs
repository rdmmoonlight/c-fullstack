using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AumoFinance.Services;

namespace AumoFinance.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderBroadcastReceiver : BroadcastReceiver
{
    public const string ActionShowReminder = "com.aumofinance.ACTION_SHOW_REMINDER";
    private const int NotificationId = 1001;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
        {
            return;
        }

        if (intent.Action != ActionShowReminder)
        {
            return;
        }

        NotificationService.EnsureChannel();

        string? packageName = context.PackageName;
        PendingIntent? pendingIntent = null;

        if (!string.IsNullOrEmpty(packageName) && context.PackageManager != null)
        {
            Intent? launchIntent = context.PackageManager.GetLaunchIntentForPackage(packageName);
            if (launchIntent != null)
            {
                pendingIntent = PendingIntent.GetActivity(
                    context,
                    0,
                    launchIntent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            }
        }

        // Hindari method chaining berantai untuk mencegah CS8602 pada Roslyn
        var builder = new NotificationCompat.Builder(context, NotificationService.ChannelId);
        builder.SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo);
        builder.SetContentTitle("AumoFinance");
        builder.SetContentText("Jangan lupa catat transaksi keuanganmu hari ini!");
        builder.SetAutoCancel(true);
        builder.SetPriority(NotificationCompat.PriorityDefault);

        if (pendingIntent != null)
        {
            builder.SetContentIntent(pendingIntent);
        }

        // Baris 56 & 60: Pengecekan eksplisit pada NotificationManager & Builder
        var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        var notification = builder.Build();

        if (notificationManager != null && notification != null)
        {
            notificationManager.Notify(NotificationId, notification);
        }
    }
}
