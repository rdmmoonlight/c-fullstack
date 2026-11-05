using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace AumoFinance.ViewModels;

public partial class JournalEntryViewModel
{
    private bool _isLocked;
    private bool _isBusy;
    private bool _isEditingMode;
    private string _pageTitle = "New Journal Entry";
    private string _submitButtonText = "Save Journal Entry";
    private string _selectedJournalType = "General";
    // DateTime.Today ber-Kind=Local — bisa memicu bug yang sama dengan
    // EntryDate/CreatedAt di SaveAsCreateAsync kalau user save tanpa
    // menyentuh date picker sama sekali. Pakai Unspecified dari awal.
    private DateTime _entryDate = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Unspecified);
    private string _transactionNumber = "Auto-generated";
    private Color _transactionNumberColor = Color.FromArgb("#D8D8D8");
    private string _totalDebitText = "Rp 0";
    private string _totalCreditText = "Rp 0";
    private bool _isBalanced;
    private bool _isSaveVisible;
    private Color _balanceBadgeBg = Color.FromArgb("#1E121F");
    private string _balanceStatusText = "UNBALANCED";
    private Color _balanceStatusTextColor = Color.FromArgb("#D7192F");

    public string PageTitle
    {
        get => _pageTitle;
        set { _pageTitle = value; OnPropertyChanged(); }
    }

    public string SubmitButtonText
    {
        get => _submitButtonText;
        set { _submitButtonText = value; OnPropertyChanged(); }
    }

    public string SelectedJournalType
    {
        get => _selectedJournalType;
        set
        {
            if (_selectedJournalType != value)
            {
                _selectedJournalType = value;
                OnPropertyChanged();
                if (!_editingEntryId.HasValue)
                {
                    _ = RefreshNextTransactionNumberAsync();
                }
            }
        }
    }

    public DateTime EntryDate
    {
        get => _entryDate;
        set { _entryDate = value; OnPropertyChanged(); }
    }

    public string TransactionNumber
    {
        get => _transactionNumber;
        set { _transactionNumber = value; OnPropertyChanged(); }
    }

    public Color TransactionNumberColor
    {
        get => _transactionNumberColor;
        set { _transactionNumberColor = value; OnPropertyChanged(); }
    }

    public string TotalDebitText
    {
        get => _totalDebitText;
        set { _totalDebitText = value; OnPropertyChanged(); }
    }

    public string TotalCreditText
    {
        get => _totalCreditText;
        set { _totalCreditText = value; OnPropertyChanged(); }
    }

    public bool IsBalanced
    {
        get => _isBalanced;
        set
        {
            _isBalanced = value;
            OnPropertyChanged();
            SaveJournalCommand.ChangeCanExecute();
        }
    }

    public bool IsSaveVisible
    {
        get => _isSaveVisible;
        set { _isSaveVisible = value; OnPropertyChanged(); }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            _isLocked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditable));
            SaveJournalCommand.ChangeCanExecute();
        }
    }

    public bool IsEditable => !IsLocked;

    public bool IsEditingMode
    {
        get => _isEditingMode;
        set { _isEditingMode = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            SaveJournalCommand.ChangeCanExecute();
        }
    }

    public Color BalanceBadgeBg
    {
        get => _balanceBadgeBg;
        set { _balanceBadgeBg = value; OnPropertyChanged(); }
    }

    public string BalanceStatusText
    {
        get => _balanceStatusText;
        set { _balanceStatusText = value; OnPropertyChanged(); }
    }

    public Color BalanceStatusTextColor
    {
        get => _balanceStatusTextColor;
        set { _balanceStatusTextColor = value; OnPropertyChanged(); }
    }
}
