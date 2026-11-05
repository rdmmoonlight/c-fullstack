using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using AumoFinance.Services;
using AumoFinance.Pages.Log;

namespace AumoFinance.Pages.Auth;

public partial class LoginPage : ContentPage
{
    private const string IconEyeVisible = "\uea9a";
    private const string IconEyeHidden = "\uecf0";

    private const string KeyRememberMe = "remember_me";
    private const string KeySavedEmail = "saved_email";
    private const string KeySavedPassword = "saved_password";

    private readonly AuthService _authService;

    public LoginPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Menonaktifkan menu samping saat di halaman login
        if (Shell.Current is Shell shell)
        {
            Shell.SetFlyoutBehavior(shell, FlyoutBehavior.Disabled);
        }

        // Cek log crash
        string? lastCrash = CrashLogger.ReadAndClearLastCrash();
        if (!string.IsNullOrWhiteSpace(lastCrash))
        {
            await Navigation.PushModalAsync(new CrashLogPage(lastCrash));
        }

        await LoadSavedCredentialsAsync();
    }

    private async Task LoadSavedCredentialsAsync()
    {
        bool isRemembered = Preferences.Default.Get(KeyRememberMe, false);
        RememberMeCheckBox.IsChecked = isRemembered;

        if (!isRemembered)
        {
            BiometricButton.IsVisible = false;
            return;
        }

        try
        {
            string? savedEmail = await SecureStorage.Default.GetAsync(KeySavedEmail);
            string? savedPassword = await SecureStorage.Default.GetAsync(KeySavedPassword);

            if (!string.IsNullOrEmpty(savedEmail)) EmailEntry.Text = savedEmail;
            if (!string.IsNullOrEmpty(savedPassword)) PasswordEntry.Text = savedPassword;

            // Tombol biometrik hanya muncul jika ada data tersimpan & hardware mendukung
            bool hasValidCredentials = !string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword);
            bool isBiometricAvailable = await CrossFingerprint.Current.IsAvailableAsync();

            BiometricButton.IsVisible = hasValidCredentials && isBiometricAvailable;
        }
        catch
        {
            BiometricButton.IsVisible = false;
        }
    }

    private void OnRememberMeLabelTapped(object? sender, TappedEventArgs e)
    {
        RememberMeCheckBox.IsChecked = !RememberMeCheckBox.IsChecked;
    }

    private void OnTogglePasswordClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        TogglePasswordButton.Text = PasswordEntry.IsPassword ? IconEyeHidden : IconEyeVisible;
    }

    private async void OnLoginButtonClicked(object? sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Email and Password cannot be empty.");
            return;
        }

        await ProcessLoginAsync(email, password);
    }

    private async void OnBiometricButtonClicked(object? sender, EventArgs e)
    {
        bool isAuthenticated = await AuthenticateWithBiometricsAsync();
        if (!isAuthenticated) return;

        try
        {
            string? savedEmail = await SecureStorage.Default.GetAsync(KeySavedEmail);
            string? savedPassword = await SecureStorage.Default.GetAsync(KeySavedPassword);

            if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword))
            {
                await ProcessLoginAsync(savedEmail, savedPassword);
            }
            else
            {
                ShowError("Saved credentials not found.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }

    private async Task<bool> AuthenticateWithBiometricsAsync()
    {
        try
        {
            if (!await CrossFingerprint.Current.IsAvailableAsync())
            {
                ShowError("Biometric not available.");
                return false;
            }

            var request = new AuthenticationRequestConfiguration("Biometric Login", "Scan your finger/face")
            {
                CancelTitle = "Cancel",
                FallbackTitle = "Use Password"
            };

            var result = await CrossFingerprint.Current.AuthenticateAsync(request);
            return result.Authenticated;
        }
        catch
        {
            return false;
        }
    }

    private async Task ProcessLoginAsync(string email, string password)
    {
        SetLoadingState(true);

        try
        {
            var (success, message, userId, fullName) = await _authService.LoginAsync(email, password);

            if (success && userId != null)
            {
                Preferences.Default.Set("current_user_id", userId);
                if (!string.IsNullOrEmpty(fullName)) Preferences.Default.Set("current_user_name", fullName);

                if (RememberMeCheckBox.IsChecked)
                {
                    Preferences.Default.Set(KeyRememberMe, true);
                    await SecureStorage.Default.SetAsync(KeySavedEmail, email);
                    await SecureStorage.Default.SetAsync(KeySavedPassword, password);
                }
                else
                {
                    Preferences.Default.Remove(KeyRememberMe);
                    SecureStorage.Default.Remove(KeySavedEmail);
                    SecureStorage.Default.Remove(KeySavedPassword);
                }

                if (Shell.Current is Shell shell)
                {
                    Shell.SetFlyoutBehavior(shell, FlyoutBehavior.Flyout);
                    await shell.GoToAsync("//HomePage");
                }
            }
            else
            {
                ShowError(message ?? "Login failed.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorCard.IsVisible = true;
    }

    private void SetLoadingState(bool isLoading)
    {
        ErrorCard.IsVisible = false;
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;
        
        LoginButton.IsEnabled = !isLoading;
        LoginButton.IsVisible = !isLoading;
        BiometricButton.IsEnabled = !isLoading;
    }
}
