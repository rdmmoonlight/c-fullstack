using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using AumoFinance.Pages.JournalEntry;
using AumoFinance.Services;
using AumoFinance.Views; // Tambahkan namespace Views untuk TopBarView

namespace AumoFinance.ViewModels;

public partial class JournalEntryViewModel : BindableObject
{
    private readonly JournalEntryService _journalEntryService;
    private readonly CoaService _coaService;
    private readonly PeriodService _periodService;
    private readonly CultureInfo _idCulture = new("id-ID");

    private List<AccountLookupDto> _allAccounts = new();
    private int? _editingEntryId;

    public JournalEntryViewModel(
        JournalEntryService journalEntryService,
        CoaService coaService,
        PeriodService periodService)
    {
        _journalEntryService = journalEntryService;
        _coaService = coaService;
        _periodService = periodService;

        Lines = new ObservableCollection<JournalLineViewModel>();

        AddLineCommand = new Command(AddNewLine);
        RemoveLineCommand = new Command<JournalLineViewModel>(RemoveLine);
        SaveJournalCommand = new Command(async () => await SaveJournalAsync(), () => !IsBusy && IsBalanced && !IsLocked);
        CancelCommand = new Command(async () => await CancelAsync());
    }

    public ObservableCollection<JournalLineViewModel> Lines { get; }
    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public Command SaveJournalCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task CancelAsync()
    {
        if (RequestNavigationPop != null)
            await RequestNavigationPop.Invoke();
    }

    public event Func<string, string, string, Task>? RequestAlert;
    public event Func<Task>? RequestNavigationPop;

    public async Task InitializeAsync(string? rawEntryId)
    {
        ResetFormLines();

        bool isEditRequest = int.TryParse(rawEntryId, out var parsedId) && parsedId > 0;

        if (isEditRequest && _editingEntryId != parsedId)
        {
            _editingEntryId = parsedId;
            await LoadAccountsAsync();
            await LoadEntryForEditAsync(parsedId);
        }
        else if (!isEditRequest && _editingEntryId == null)
        {
            await LoadAccountsAsync();
            await RefreshNextTransactionNumberAsync();
        }
    }

    // Menggunakan TopBarView secara eksplisit (atau aman via casting) untuk mencegah error CS1503
    public async Task ApplyPeriodHeaderAsync(object topHeader)
    {
        if (topHeader is TopBarView topBar)
        {
            await SelectedPeriodDisplayHelper.ApplyToTopBarAsync(topBar, _periodService);
        }
    }

    private void AddNewLine()
    {
        var newLine = new JournalLineViewModel(_allAccounts, UpdateTotals);
        Lines.Add(newLine);
        UpdateTotals();
    }

    private void RemoveLine(JournalLineViewModel? lineVm)
    {
        if (lineVm != null && Lines.Contains(lineVm))
        {
            Lines.Remove(lineVm);
            UpdateTotals();
        }
    }

    private void ResetFormLines()
    {
        Lines.Clear();
        AddNewLine();
        AddNewLine();
    }
}
