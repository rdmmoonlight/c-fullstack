using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace AumoFinance.Pages.Auth;

public partial class LogoutPage : ContentPage
{
    public LogoutPage()
    {
        InitializeComponent();
    }

    private async void OnLogoutButtonClicked(object? sender, EventArgs e)
    {
        SetLoadingState(true);

        try
        {
            // 1. Hapus Data Sesi User dari Preferences
            Preferences.Default.Remove("current_user_id");
            Preferences.Default.Remove("current_user_name");

            // 2. Kunci Kembali Flyout Menu Drawer & Navigasi ke LoginPage
            if (Shell.Current is Shell shell)
            {
                Shell.SetFlyoutBehavior(shell, FlyoutBehavior.Disabled);
                await shell.GoToAsync("//LoginPage");
            }
        }
        catch (Exception ex)
        {
            // Menggunakan this.DisplayAlertAsync (bukan Application.Current.MainPage) agar konsisten
            await this.DisplayAlertAsync("Error", $"Gagal melakukan logout: {ex.Message}", "OK");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void OnCancelButtonClicked(object? sender, EventArgs e)
    {
        if (Shell.Current is Shell shell)
        {
            await shell.GoToAsync("..");
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;
        LogoutButton.IsEnabled = !isLoading;
        LogoutButton.IsVisible = !isLoading;
        CancelButton.IsEnabled = !isLoading;
    }
}
