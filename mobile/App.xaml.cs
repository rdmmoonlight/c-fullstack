using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using AumoFinance.Services;

namespace AumoFinance;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Jalankan pengecekan update otomatis di background thread saat startup
        Task.Run(async () =>
        {
            try
            {
                // Cek status sakelar Auto-Update di Preferences (default: true)
                bool isAutoUpdateEnabled = Preferences.Default.Get("AutoUpdateEnabled", true);

                if (isAutoUpdateEnabled)
                {
                    // Gunakan IServiceProvider jika UpdateService terdaftar di DI Container,
                    // atau fallback ke instansiasi manual jika belum terdaftar.
                    var updateService = _serviceProvider.GetService<UpdateService>() ?? new UpdateService();

                    // Memicu pencarian & instalasi otomatis (isSilent: true)
                    await updateService.CheckAndInstallUpdateAsync(
                        githubUser: "rdmmoonlight",
                        githubRepo: "aumo-finance-android",
                        isSilent: true
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App AutoUpdate Exception] {ex.Message}");
            }
        });

        // Resolve AppShell dari ServiceProvider agar mendukung Dependency Injection pada Pages/ViewModels
        var appShell = _serviceProvider.GetService<AppShell>() ?? new AppShell();
        return new Window(appShell);
    }
}