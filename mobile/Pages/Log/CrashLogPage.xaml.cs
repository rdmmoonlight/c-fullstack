using System;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;

namespace AumoFinance.Pages.Log;

public partial class CrashLogPage : ContentPage
{
    private readonly string _logText;

    public CrashLogPage(string logText)
    {
        InitializeComponent();
        _logText = logText ?? string.Empty;
        LogLabel.Text = _logText;
    }

    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(_logText);
        await DisplayAlert("Berhasil", "Log crash disalin ke clipboard.", "OK");
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
