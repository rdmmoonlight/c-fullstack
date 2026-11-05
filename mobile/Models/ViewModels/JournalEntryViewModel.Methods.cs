using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using AumoFinance.Pages.JournalEntry;
using AumoFinance.Services;

namespace AumoFinance.ViewModels;

public partial class JournalEntryViewModel
{
    private async Task LoadAccountsAsync()
    {
        try
        {
            var (accounts, errorDetail) = await _coaService.GetAccountsAsync();
            if (accounts != null && accounts.Any())
            {
                _allAccounts = accounts.Select(a => new AccountLookupDto
                {
                    Id = a.Id,
                    ReferenceNumber = a.ReferenceNumber,
                    AccountName = a.AccountName,
                    DisplayName = $"{a.ReferenceNumber} - {a.AccountName}"
                }).ToList();

                foreach (var line in Lines)
                {
                    line.AvailableAccounts = _allAccounts;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LoadAccountsAsync Exception: {ex}");
        }
    }

    private async Task LoadEntryForEditAsync(int entryId)
    {
        IsSaveVisible = false;

        var (entry, errorDetail) = await _journalEntryService.GetJournalEntryByIdAsync(entryId);

        if (!string.IsNullOrEmpty(errorDetail) || entry == null)
        {
            if (RequestAlert != null)
                await RequestAlert.Invoke("Error", errorDetail ?? "Journal entry not found.", "OK");

            if (RequestNavigationPop != null)
                await RequestNavigationPop.Invoke();
            return;
        }

        PageTitle = "Edit Journal Entry";
        SubmitButtonText = "Update Journal Entry";
        IsEditingMode = true;

        SelectedJournalType = entry.JournalType;
        // Lucuti Kind dari nilai yang datang dari server juga, supaya kalau
        // entry ini di-Update tanpa user menyentuh date picker, tidak ikut
        // membawa Kind=Utc/offset yang berpotensi memicu masalah serupa.
        EntryDate = DateTime.SpecifyKind(entry.EntryDate.Date, DateTimeKind.Unspecified);

        TransactionNumber = TransactionNumberDisplayHelper.Format(entry.TransactionNumber);
        TransactionNumberColor = Colors.White;

        Lines.Clear();
        foreach (var line in entry.Lines.OrderBy(l => l.LineOrder))
        {
            var lineVm = new JournalLineViewModel(_allAccounts, UpdateTotals)
            {
                SelectedAccount = _allAccounts.FirstOrDefault(a => a.Id == line.AccountId),
                DebitText = line.Debit > 0 ? string.Format(_idCulture, "{0:N0}", line.Debit) : string.Empty,
                CreditText = line.Credit > 0 ? string.Format(_idCulture, "{0:N0}", line.Credit) : string.Empty,
                LineDescription = line.LineDescription ?? string.Empty
            };
            Lines.Add(lineVm);
        }

        IsLocked = entry.IsLocked;
        UpdateTotals();
    }

    private async Task RefreshNextTransactionNumberAsync()
    {
        TransactionNumber = "Loading...";
        TransactionNumberColor = Color.FromArgb("#D8D8D8");

        // Preview saja, tidak mengonsumsi nomor — server hanya membaca
        // counter tanpa menaikkannya. Nomor final tetap diambil ulang
        // secara atomik oleh server saat entry ini benar-benar disimpan.
        var (nextNumber, _) = await _journalEntryService.GetNextTransactionNumberAsync(SelectedJournalType);

        if (!string.IsNullOrWhiteSpace(nextNumber))
        {
            TransactionNumber = TransactionNumberDisplayHelper.Format(nextNumber);
            TransactionNumberColor = Colors.White;
        }
        else
        {
            TransactionNumber = "Auto-generated";
            TransactionNumberColor = Color.FromArgb("#D8D8D8");
        }
    }

    private void UpdateTotals()
    {
        decimal totalDebit = Lines.Sum(l => l.Debit);
        decimal totalCredit = Lines.Sum(l => l.Credit);

        TotalDebitText = string.Format(_idCulture, "Rp {0:N0}", totalDebit);
        TotalCreditText = string.Format(_idCulture, "Rp {0:N0}", totalCredit);

        IsBalanced = Math.Round(totalDebit - totalCredit, 2) == 0 && totalDebit > 0;

        if (IsBalanced)
        {
            BalanceBadgeBg = Color.FromArgb("#1E121F");
            BalanceStatusText = "BALANCED";
            BalanceStatusTextColor = Color.FromArgb("#4FA36A");
        }
        else
        {
            BalanceBadgeBg = Color.FromArgb("#1E121F");
            BalanceStatusText = "UNBALANCED";
            BalanceStatusTextColor = Color.FromArgb("#D7192F");
        }

        IsSaveVisible = IsBalanced && !IsLocked;
    }

    private async Task SaveJournalAsync()
    {
        if (IsLocked) return;

        var isValid = await ValidateFormAsync();
        if (!isValid) return;

        IsBusy = true;

        try
        {
            if (_editingEntryId.HasValue)
            {
                await SaveAsUpdateAsync(_editingEntryId.Value);
            }
            else
            {
                await SaveAsCreateAsync();
            }
        }
        catch (Exception ex)
        {
            if (RequestAlert != null)
                await RequestAlert.Invoke("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            UpdateTotals();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsCreateAsync()
    {
        var requestDto = new CreateJournalEntryRequest
        {
            JournalType = SelectedJournalType,
            // DateTimeKind dilucuti (Unspecified) sebelum dikirim. Root cause
            // bug "tanggal mundur sehari": DateTime.Today/DateTime.Now punya
            // Kind=Local, System.Text.Json men-serialize itu dengan offset
            // device (mis. +07:00). Saat backend (server jalan di UTC)
            // mem-parsing string berooffset non-nol ke tipe DateTime, .NET
            // benar-benar MENGKONVERSI jam-nya ke waktu lokal server (00:00
            // WIB - 7 jam = 17:00 hari sebelumnya, UTC) — bukan cuma
            // label Kind yang berubah. Makanya tanggal 15 jadi 14.
            // Kirim tanpa offset (Unspecified) supaya angka wall-clock-nya
            // sampai apa adanya, sama seperti perlakuan di web (Blazor).
            EntryDate = DateTime.SpecifyKind(EntryDate.Date, DateTimeKind.Unspecified),
            CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
            Lines = Lines
                .Where(l => l.SelectedAccount != null && (l.Debit > 0 || l.Credit > 0))
                .Select(l => new JournalEntryLineRequest
                {
                    AccountId = l.SelectedAccount!.Id,
                    LineDescription = l.LineDescription,
                    Debit = l.Debit,
                    Credit = l.Credit
                }).ToList()
        };

        var (success, message, _, transactionNumber) = await _journalEntryService.CreateJournalEntryAsync(requestDto);

        if (success)
        {
            string successMessage = string.IsNullOrWhiteSpace(transactionNumber)
                ? message
                : $"Journal Entry {TransactionNumberDisplayHelper.Format(transactionNumber)} recorded successfully!";

            RememberLineDescriptions();

            if (RequestAlert != null)
                await RequestAlert.Invoke("Success", successMessage, "OK");

            ResetFormLines();
            await RefreshNextTransactionNumberAsync();
        }
        else
        {
            if (RequestAlert != null)
                await RequestAlert.Invoke("Posting Failed", message, "OK");
            UpdateTotals();
        }
    }

    private async Task SaveAsUpdateAsync(int entryId)
    {
        var updateDto = new UpdateJournalEntryRequest
        {
            JournalType = SelectedJournalType,
            // Sama seperti SaveAsCreateAsync — lucuti Kind supaya tidak
            // kena konversi timezone nyata saat backend mem-parsing (lihat
            // komentar lengkap di SaveAsCreateAsync).
            EntryDate = DateTime.SpecifyKind(EntryDate.Date, DateTimeKind.Unspecified),
            // UpdatedAt = device wall-clock time when this edit is saved
            // (the 3rd timestamp, separate from CreatedAt which is only
            // set once when the entry is first created).
            UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
            Lines = Lines
                .Where(l => l.SelectedAccount != null && (l.Debit > 0 || l.Credit > 0))
                .Select(l => new JournalEntryLineRequest
                {
                    AccountId = l.SelectedAccount!.Id,
                    LineDescription = l.LineDescription,
                    Debit = l.Debit,
                    Credit = l.Credit
                }).ToList()
        };

        var (success, message) = await _journalEntryService.EditJournalEntryAsync(entryId, updateDto);

        if (success)
        {
            RememberLineDescriptions();

            if (RequestAlert != null)
                await RequestAlert.Invoke("Success", message, "OK");

            if (RequestNavigationPop != null)
                await RequestNavigationPop.Invoke();
        }
        else
        {
            if (RequestAlert != null)
                await RequestAlert.Invoke("Update Failed", message, "OK");
            UpdateTotals();
        }
    }

    private void RememberLineDescriptions()
    {
        foreach (var line in Lines)
            DescriptionSuggestionService.Remember(line.LineDescription);
    }

    private async Task<bool> ValidateFormAsync()
    {
        var activeLines = Lines.Where(l => l.SelectedAccount != null && (l.Debit > 0 || l.Credit > 0)).ToList();
        decimal totalDebit = activeLines.Sum(l => l.Debit);
        decimal totalCredit = activeLines.Sum(l => l.Credit);

        if (activeLines.Count < 2)
        {
            if (RequestAlert != null)
                await RequestAlert.Invoke("Validation Error", "A journal entry must have at least 2 active transaction lines with valid accounts.", "OK");
            return false;
        }

        foreach (var line in activeLines)
        {
            if (line.Debit > 0 && line.Credit > 0)
            {
                if (RequestAlert != null)
                    await RequestAlert.Invoke("Validation Error", "A single transaction line cannot have both Debit and Credit amounts.", "OK");
                return false;
            }
        }

        if (Math.Round(totalDebit - totalCredit, 2) != 0 || totalDebit == 0)
        {
            if (RequestAlert != null)
                await RequestAlert.Invoke("Validation Error", "Total debits must equal total credits and be greater than zero before saving.", "OK");
            return false;
        }

        return true;
    }
}
