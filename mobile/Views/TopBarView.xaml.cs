using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;

namespace AumoFinance.Views;

public partial class TopBarView : ContentView
{
    private int _pendingQueueCount = 0;

    public TopBarView()
    {
        InitializeComponent();
    }

    public string PeriodText
    {
        get => PeriodLabel.Text;
        set => PeriodLabel.Text = string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

    /// <summary>
    /// Menampilkan ikon gembok kecil di samping nama periode saat periode tersebut
    /// sudah ditutup (closed). Menggantikan pendekatan lama yang menempelkan emoji 🔒
    /// langsung ke dalam teks PeriodText.
    /// </summary>
    public bool IsPeriodLocked
    {
        get => PeriodLockIcon.IsVisible;
        set => PeriodLockIcon.IsVisible = value;
    }

    /// <summary>
    /// Properti untuk meng-update jumlah antrean transaksi lokal yang belum tersinkron.
    /// Panggil properti ini setiap kali ada jurnal baru yang disimpan lokal atau sukses di-upload.
    /// </summary>
    public int PendingQueueCount
    {
        get => _pendingQueueCount;
        set
        {
            _pendingQueueCount = Math.Max(0, value);
            UpdateNetworkAndQueueUI();
        }
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        // Mulai memantau perubahan jaringan saat komponen dipasang
        if (Parent != null)
        {
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
            UpdateNetworkAndQueueUI();
        }
        else
        {
            // Lepas event handler saat komponen di-unmount dari layar
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        // Jalankan update UI di Main/UI Thread agar aman
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateNetworkAndQueueUI();
        });
    }

    private const string IconWifi = "\uEB52";
    private const string IconWifiOff = "\uECFA";
    private const string IconRefresh = "\uEB13";

    private void UpdateNetworkAndQueueUI()
    {
        bool isConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        if (!isConnected)
        {
            // Tampilan saat OFFLINE — hanya ikon, teks "Offline" dihapus
            ConnectionStatusIcon.Text = IconWifiOff;
            ConnectionStatusIcon.TextColor = Color.Parse("#D7192F");
            string tooltip = _pendingQueueCount > 0
                ? $"Offline ({_pendingQueueCount} pending)"
                : "Offline";
            ToolTipProperties.SetText(ConnectionStatusIcon, tooltip);
        }
        else
        {
            if (_pendingQueueCount > 0)
            {
                // Ada antrean yang sedang/akan di-sync
                ConnectionStatusIcon.Text = IconRefresh;
                ConnectionStatusIcon.TextColor = Color.Parse("#D8D8D8");
                ToolTipProperties.SetText(ConnectionStatusIcon, $"Syncing ({_pendingQueueCount})...");
            }
            else
            {
                // Online & semua data tersinkron sempurna — hanya ikon, teks "Online" dihapus
                ConnectionStatusIcon.Text = IconWifi;
                ConnectionStatusIcon.TextColor = Color.Parse("#4FA36A");
                ToolTipProperties.SetText(ConnectionStatusIcon, "Online");
            }
        }
    }

    private void OnMenuButtonClicked(object? sender, EventArgs e)
    {
        var shell = Shell.Current;
        if (shell != null)
        {
            shell.FlyoutIsPresented = !shell.FlyoutIsPresented;
        }
    }
}
