using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using AumoFinance.Pages.JournalEntry;
using AumoFinance.Services;

namespace AumoFinance.ViewModels;

public class JournalLineViewModel : BindableObject
{
    private readonly Action _onChanged;
    private List<AccountLookupDto> _availableAccounts;
    private AccountLookupDto? _selectedAccount;
    private string _debitText = string.Empty;
    private string _creditText = string.Empty;
    private string _lineDescription = string.Empty;
    private bool _showSuggestions;
    private readonly CultureInfo _idCulture = new("id-ID");

    public JournalLineViewModel(List<AccountLookupDto> availableAccounts, Action onChanged)
    {
        _availableAccounts = availableAccounts;
        _onChanged = onChanged;
        SelectSuggestionCommand = new Command<string>(ApplySuggestion);
    }

    public ICommand SelectSuggestionCommand { get; }
    public ObservableCollection<string> DescriptionSuggestions { get; } = new();

    public bool ShowSuggestions
    {
        get => _showSuggestions;
        set { _showSuggestions = value; OnPropertyChanged(); }
    }

    public List<AccountLookupDto> AvailableAccounts
    {
        get => _availableAccounts;
        set { _availableAccounts = value; OnPropertyChanged(); }
    }

    public AccountLookupDto? SelectedAccount
    {
        get => _selectedAccount;
        set { _selectedAccount = value; OnPropertyChanged(); }
    }

    public string DebitText
    {
        get => _debitText;
        set
        {
            string formatted = FormatThousandSeparator(value);
            if (_debitText != formatted)
            {
                _debitText = formatted;
                OnPropertyChanged();
                _onChanged?.Invoke();
            }
        }
    }

    public string CreditText
    {
        get => _creditText;
        set
        {
            string formatted = FormatThousandSeparator(value);
            if (_creditText != formatted)
            {
                _creditText = formatted;
                OnPropertyChanged();
                _onChanged?.Invoke();
            }
        }
    }

    public string LineDescription
    {
        get => _lineDescription;
        set
        {
            _lineDescription = value;
            OnPropertyChanged();
            RefreshSuggestions();
        }
    }

    private void RefreshSuggestions()
    {
        DescriptionSuggestions.Clear();

        foreach (var match in DescriptionSuggestionService.GetSuggestions(_lineDescription))
            DescriptionSuggestions.Add(match);

        ShowSuggestions = DescriptionSuggestions.Count > 0;
    }

    private void ApplySuggestion(string suggestion)
    {
        if (string.IsNullOrEmpty(suggestion)) return;
        LineDescription = suggestion; // otomatis membersihkan & menyembunyikan daftar saran
        ShowSuggestions = false;
        DescriptionSuggestions.Clear();
    }

    public decimal Debit => ParseDecimal(_debitText);
    public decimal Credit => ParseDecimal(_creditText);

    private string FormatThousandSeparator(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string digitsOnly = Regex.Replace(input, @"[^\d]", "");
        if (decimal.TryParse(digitsOnly, out decimal value))
        {
            return value.ToString("N0", _idCulture);
        }
        return string.Empty;
    }

    private decimal ParseDecimal(string formattedInput)
    {
        if (string.IsNullOrWhiteSpace(formattedInput)) return 0m;
        string rawDigits = Regex.Replace(formattedInput, @"[^\d]", "");
        return decimal.TryParse(rawDigits, out decimal val) ? val : 0m;
    }
}