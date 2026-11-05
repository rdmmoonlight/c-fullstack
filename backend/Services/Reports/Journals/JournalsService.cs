using System.Globalization;
using AumoBackend.Data;
using AumoBackend.DTOs.Reports;
using AumoBackend.Helpers;
using AumoBackend.Models;
using AumoBackend.Services.JournalEntries;
using AumoBackend.Services.Reports.TrialBalances;
using Microsoft.EntityFrameworkCore;

namespace AumoBackend.Services.Reports.Journals;

public class JournalsService : IJournalsService
{
    private const string AdjustingType = "Adjusting";

    private readonly AppDbContext _db;
    private readonly ITrialBalancesService _trialBalancesService;
    private readonly IJournalEntryService _journalEntryService;

    public JournalsService(
        AppDbContext db,
        ITrialBalancesService trialBalancesService,
        IJournalEntryService journalEntryService)
    {
        _db = db;
        _trialBalancesService = trialBalancesService;
        _journalEntryService = journalEntryService;
    }

    public async Task<JournalReportResult> GetJournalAsync(Guid userId, string journalType)
    {
        var period = await SelectedPeriodHelper.GetSelectedPeriodAsync(_db, userId);
        if (period == null)
        {
            return new JournalReportResult { HasPeriodSelected = false };
        }

        var startUtc = DateTime.SpecifyKind(period.StartDate.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(period.EndDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var rows = await _db.GeneralJournals
            .AsNoTracking()
            .Include(g => g.Account)
            .Where(g => g.UserId == userId
                     && g.JournalType == journalType
                     && g.EntryDate >= startUtc
                     && g.EntryDate <= endUtc)
            .ToListAsync();

        var entries = rows
            .GroupBy(g => g.JournalEntryId)
            .Select(grp => new { Header = grp.First(), Lines = grp })
            .OrderBy(x => x.Header.EntryDate)
            .ThenBy(x => x.Header.CreatedAt)
            .ThenBy(x => x.Header.JournalEntryId)
            .Select(x => new JournalReportEntryDto
            {
                Id = x.Header.JournalEntryId,
                TransactionNumber = x.Header.TransactionNumber,
                JournalType = x.Header.JournalType,
                EntryDate = x.Header.EntryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                CreatedAt = x.Header.CreatedAt,
                UpdatedAt = x.Header.UpdatedAt,
                Lines = x.Lines
                    .OrderBy(l => l.LineOrder)
                    .Select(l => new JournalReportLineDto
                    {
                        Id = l.LineId,
                        AccountId = l.AccountId,
                        AccountName = l.Account != null ? l.Account.AccountName : "Unknown",
                        ReferenceNumber = l.Account != null ? l.Account.ReferenceNumber.ToString(CultureInfo.InvariantCulture) : "0",
                        LineDescription = l.LineDescription,
                        Debit = l.Debit,
                        Credit = l.Credit,
                        LineOrder = l.LineOrder,
                    })
                    .ToList(),
            })
            .ToList();

        return new JournalReportResult
        {
            HasPeriodSelected = true,
            SelectedPeriodName = period.PeriodName,
            IsPeriodClosed = period.IsClosed,
            Entries = entries,
        };
    }

    public async Task DeleteAdjustingAsync(Guid userId, int journalEntryId)
    {
        var exists = await _db.GeneralJournals
            .AsNoTracking()
            .AnyAsync(g => g.JournalEntryId == journalEntryId
                        && g.UserId == userId
                        && g.JournalType == AdjustingType);

        if (!exists)
        {
            throw new KeyNotFoundException("Adjusting journal entry not found.");
        }

        await _journalEntryService.DeleteAsync(userId, journalEntryId);
    }

    public async Task<ClosingJournalReportResult> GetClosingJournalAsync(Guid userId)
    {
        var period = await SelectedPeriodHelper.GetSelectedPeriodAsync(_db, userId);
        if (period == null)
        {
            return new ClosingJournalReportResult { HasPeriodSelected = false };
        }

        // Jurnal penutup baru ada setelah periode ditutup: sebelum itu tidak dihitung sama sekali.
        if (!period.IsClosed)
        {
            return new ClosingJournalReportResult
            {
                HasPeriodSelected = true,
                SelectedPeriodName = period.PeriodName,
                IsPeriodClosed = false,
            };
        }

        var (groups, netIncome, reAccountName) = await ComputeClosingAsync(userId, period);

        return new ClosingJournalReportResult
        {
            HasPeriodSelected = true,
            SelectedPeriodName = period.PeriodName,
            IsPeriodClosed = period.IsClosed,
            NetIncome = netIncome,
            RetainedEarningsAccountName = reAccountName,
            Groups = groups,
        };
    }

    public async Task<List<ClosingJournalEntryGroupApiResponse>> BuildClosingJournalGroupsAsync(
        Guid userId,
        Period period)
    {
        // Pengecekan agar pembentukan grup jurnal penutup hanya diproses saat periode sudah ditutup.
        if (period == null || !period.IsClosed)
        {
            return new List<ClosingJournalEntryGroupApiResponse>();
        }

        var (groups, _, _) = await ComputeClosingAsync(userId, period);
        return groups;
    }

    private async Task<(List<ClosingJournalEntryGroupApiResponse> Groups, decimal NetIncome, string ReAccountName)> ComputeClosingAsync(
        Guid userId,
        Period period)
    {
        var rows = await _trialBalancesService.BuildTrialBalancesRowsAsync(userId, period, true);

        // Baris trial balance hanya memuat akun yang bergerak pada periode ini, jadi nama Laba Ditahan
        // diambil langsung dari bagan akun agar tetap benar walau akun itu belum bertransaksi.
        var reAccountName = await _db.ChartOfAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive && a.Role == "RetainedEarnings")
            .OrderBy(a => a.ReferenceNumber)
            .Select(a => a.AccountName)
            .FirstOrDefaultAsync()
            ?? rows.Find(r => r.Role == "RetainedEarnings")?.AccountName
            ?? "Retained Earnings";
        const string incomeSummaryName = "Income Summary";

        var groups = new List<ClosingJournalEntryGroupApiResponse>();

        var incomeRows = rows
            .Where(r => r.Type == "OperatingIncome" || r.Type == "OtherIncome")
            .Where(r => r.NetBalance != 0)
            .ToList();

        var expenseRows = rows
            .Where(r => r.Type == "OperatingExpenses" || r.Type == "OtherExpenses")
            .Where(r => r.NetBalance != 0)
            .ToList();

        var totalIncome = incomeRows.Sum(r => r.NetBalance);
        var totalExpenses = expenseRows.Sum(r => r.NetBalance);

        // BLOCK 1: Closing Revenues to Income Summary
        if (incomeRows.Count > 0)
        {
            var group1 = new ClosingJournalEntryGroupApiResponse
            {
                Description = "Closing Revenue Accounts to Income Summary"
            };

            foreach (var r in incomeRows)
            {
                group1.Lines.Add(new ClosingJournalLineApiResponse
                {
                    ReferenceNumber = r.ReferenceNumber ?? "0",
                    AccountName = r.AccountName,
                    Debit = r.NetBalance,
                    Credit = 0m,
                });
            }

            group1.Lines.Add(new ClosingJournalLineApiResponse
            {
                ReferenceNumber = "0",
                AccountName = incomeSummaryName,
                Debit = 0m,
                Credit = totalIncome,
            });

            groups.Add(group1);
        }

        // BLOCK 2: Closing Expenses to Income Summary
        if (expenseRows.Count > 0)
        {
            var group2 = new ClosingJournalEntryGroupApiResponse
            {
                Description = "Closing Expense Accounts to Income Summary"
            };

            group2.Lines.Add(new ClosingJournalLineApiResponse
            {
                ReferenceNumber = "0",
                AccountName = incomeSummaryName,
                Debit = totalExpenses,
                Credit = 0m,
            });

            foreach (var r in expenseRows)
            {
                group2.Lines.Add(new ClosingJournalLineApiResponse
                {
                    ReferenceNumber = r.ReferenceNumber ?? "0",
                    AccountName = r.AccountName,
                    Debit = 0m,
                    Credit = r.NetBalance,
                });
            }

            groups.Add(group2);
        }

        // BLOCK 3: Closing Income Summary to Retained Earnings
        var netIncome = totalIncome - totalExpenses;

        if (netIncome != 0)
        {
            var group3 = new ClosingJournalEntryGroupApiResponse
            {
                Description = "Closing Income Summary to Retained Earnings"
            };

            if (netIncome > 0)
            {
                group3.Lines.Add(new ClosingJournalLineApiResponse
                {
                    ReferenceNumber = "0",
                    AccountName = incomeSummaryName,
                    Debit = netIncome,
                    Credit = 0m,
                });
                group3.Lines.Add(new ClosingJournalLineApiResponse
                {
                    ReferenceNumber = "0",
                    AccountName = reAccountName,
                    Debit = 0m,
                    Credit = netIncome,
                });
            }
            else
            {
                var netLoss = Math.Abs(netIncome);
                group3.Lines.Add(new ClosingJournalLineApiResponse
                {
                    ReferenceNumber = "0",
                    AccountName = reAccountName,
                    Debit = netLoss,
                    Credit = 0m,
                });
                group3.Lines.Add(new ClosingJournalLineApiResponse
                {
                    ReferenceNumber = "0",
                    AccountName = incomeSummaryName,
                    Debit = 0m,
                    Credit = netLoss,
                });
            }

            groups.Add(group3);
        }

        return (groups, netIncome, reAccountName);
    }
}
