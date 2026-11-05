// <copyright file="ToolsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Services.Tools;

using System.Globalization;
using AumoBackend.Data;
using AumoBackend.DTOs;
using AumoBackend.Models;
using AumoBackend.Services.JournalEntries;
using AumoBackend.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

public class ToolsService(AppDbContext context) : IToolsService
{
    public byte[] GenerateJournalTemplate()
    {
        using var workbook = new XLWorkbook();

        CreateJournalSheet(workbook, "GJ");
        CreateJournalSheet(workbook, "AJ");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void CreateJournalSheet(XLWorkbook workbook, string sheetName)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Account Name";
        ws.Cell(1, 3).Value = "Description";
        ws.Cell(1, 4).Value = "Ref";
        ws.Cell(1, 5).Value = "Debit";
        ws.Cell(1, 6).Value = "Credit";
        ws.Row(1).Style.Font.Bold = true;
    }

    public async Task<PreviewJournalImportViewModel> PreviewJournalImportAsync(Guid userId, JournalImportRequestDto request)
    {
        var existingCoas = await context.ChartOfAccounts
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .ToListAsync();

        var mappingDetails = new List<AccountMappingDetailDto>();
        var processedTransactions = new List<JournalTransactionDto>();
        var counterMemory = new Dictionary<string, int>();

        foreach (var txDto in request.Transactions)
        {
            if (!DateTime.TryParse(txDto.Date, out var rawTxDate))
            {
                continue;
            }

            int day = Math.Min(rawTxDate.Day, DateTime.DaysInMonth(request.TargetYear, request.TargetMonth));
            var txDate = DateTime.SpecifyKind(new DateTime(request.TargetYear, request.TargetMonth, day), DateTimeKind.Utc);

            string prefix = txDto.JournalType.Equals("Adjusting", StringComparison.OrdinalIgnoreCase) ? "AJ" : "GJ";
            string counterKey = $"{prefix}{txDate:yyMM}";

            if (!counterMemory.ContainsKey(counterKey))
            {
                var existingCounter = await context.TransactionCounters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.CounterKey == counterKey);

                counterMemory[counterKey] = existingCounter?.LastSequence ?? 0;
            }

            counterMemory[counterKey] += 1;
            string generatedTxNumber = $"{counterKey}{counterMemory[counterKey]:D5}";

            var processedLines = new List<JournalLineDto>();

            foreach (var lineDto in txDto.Lines)
            {
                int refInt = lineDto.RefNumber;
                string excelAccountName = lineDto.AccountName?.Trim() ?? string.Empty;

                var coa = existingCoas.FirstOrDefault(c => c.ReferenceNumber == refInt && string.Equals(c.AccountName, excelAccountName, StringComparison.OrdinalIgnoreCase));

                if (coa != null)
                {
                    mappingDetails.Add(new AccountMappingDetailDto
                    {
                        ExcelRef = refInt,
                        ExcelAccountName = excelAccountName,
                        MappedRef = coa.ReferenceNumber,
                        MappedAccountName = coa.AccountName,
                        Status = "EXACT_MATCH",
                        Reason = "Nomor Ref dan Nama Akun cocok 100% presisi dengan Master COA.",
                    });
                }
                else
                {
                    mappingDetails.Add(new AccountMappingDetailDto
                    {
                        ExcelRef = refInt,
                        ExcelAccountName = excelAccountName,
                        MappedRef = 0,
                        MappedAccountName = string.Empty,
                        Status = "UNMAPPED",
                        Reason = "Silakan pilih akun pelimpahan manual dari dropdown.",
                    });
                }

                processedLines.Add(new JournalLineDto
                {
                    RefNumber = refInt,
                    AccountName = excelAccountName,
                    Description = lineDto.Description,
                    Debit = lineDto.Debit,
                    Credit = lineDto.Credit,
                });
            }

            processedTransactions.Add(new JournalTransactionDto
            {
                TransactionNumber = generatedTxNumber,
                Date = txDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                JournalType = txDto.JournalType,
                Lines = processedLines,
            });
        }

        var uniqueMappings = mappingDetails
            .GroupBy(m => new { m.ExcelRef, m.ExcelAccountName })
            .Select(g => g.First())
            .ToList();

        int exactMatchCount = uniqueMappings.Count(m => m.Status == "EXACT_MATCH");
        int reallocatedCount = uniqueMappings.Count(m => m.Status == "REALLOCATED_NAME" || m.Status == "REALLOCATED_REF");
        int unmappedCount = uniqueMappings.Count(m => m.Status == "UNMAPPED" || m.MappedRef == 0);

        return new PreviewJournalImportViewModel
        {
            Transactions = processedTransactions,
            AccountMappings = uniqueMappings,
            Summary = new PreviewJournalSummaryViewModel
            {
                TotalUniqueAccounts = uniqueMappings.Count,
                ExactMatchCount = exactMatchCount,
                ReallocatedCount = reallocatedCount,
                UnmappedCount = unmappedCount,
                IsPerfectMatch = unmappedCount == 0
            },
        };
    }

    public async Task<ImportJournalResultViewModel> ImportJournalEntriesAsync(Guid userId, JournalImportRequestDto request)
    {
        using var dbTransaction = await context.Database.BeginTransactionAsync();

        try
        {
            var period = await context.Periods.FirstOrDefaultAsync(p =>
                p.UserId == userId &&
                p.StartDate.Year == request.TargetYear &&
                p.StartDate.Month == request.TargetMonth);

            if (period == null)
            {
                var monthName = new DateTime(request.TargetYear, request.TargetMonth, 1)
                    .ToString("MMMM yyyy", new CultureInfo("id-ID"));

                period = new Period
                {
                    UserId = userId,
                    PeriodName = monthName,
                    StartDate = DateTime.SpecifyKind(new DateTime(request.TargetYear, request.TargetMonth, 1), DateTimeKind.Utc),
                    EndDate = DateTime.SpecifyKind(new DateTime(request.TargetYear, request.TargetMonth, DateTime.DaysInMonth(request.TargetYear, request.TargetMonth)), DateTimeKind.Utc),
                    IsClosed = false,
                    IsSelected = false,
                };
                context.Periods.Add(period);
                await context.SaveChangesAsync();
            }

            var existingCoas = await context.ChartOfAccounts
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync();

            var mappingDict = request.CustomMappings?
                .Where(m => m.MappedRef > 0)
                .ToDictionary(
                    m => $"{m.ExcelRef}|||{m.ExcelAccountName.Trim()}",
                    m => m,
                    StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, AccountMappingDetailDto>();

            var existingTxNumbers = await context.GeneralJournals
                .Where(j => j.UserId == userId)
                .Select(j => j.TransactionNumber)
                .Distinct()
                .ToHashSetAsync();

            var activeCounters = new Dictionary<string, TransactionCounter>();
            int importedEntriesCount = 0;

            foreach (var txDto in request.Transactions)
            {
                if (!DateTime.TryParse(txDto.Date, out var rawTxDate))
                {
                    continue;
                }

                int day = Math.Min(rawTxDate.Day, DateTime.DaysInMonth(request.TargetYear, request.TargetMonth));
                var txDate = DateTime.SpecifyKind(new DateTime(request.TargetYear, request.TargetMonth, day), DateTimeKind.Utc);

                string prefix = txDto.JournalType.Equals("Adjusting", StringComparison.OrdinalIgnoreCase) ? "AJ" : "GJ";
                string counterKey = $"{prefix}{txDate:yyMM}";

                if (!activeCounters.TryGetValue(counterKey, out var counter))
                {
                    counter = await context.TransactionCounters.FirstOrDefaultAsync(c =>
                        c.UserId == userId &&
                        c.CounterKey == counterKey);

                    if (counter == null)
                    {
                        counter = new TransactionCounter
                        {
                            UserId = userId,
                            CounterKey = counterKey,
                            LastSequence = 0,
                        };
                        context.TransactionCounters.Add(counter);
                    }

                    activeCounters[counterKey] = counter;
                }

                counter.LastSequence += 1;
                string transactionNumber = $"{counterKey}{counter.LastSequence:D5}";

                while (existingTxNumbers.Contains(transactionNumber))
                {
                    counter.LastSequence += 1;
                    transactionNumber = $"{counterKey}{counter.LastSequence:D5}";
                }

                existingTxNumbers.Add(transactionNumber);

                var importLines = new List<GeneralJournalWriter.Line>();

                foreach (var lineDto in txDto.Lines)
                {
                    int excelRef = lineDto.RefNumber;
                    string excelAccountName = lineDto.AccountName?.Trim() ?? string.Empty;

                    int targetRef = excelRef;

                    string mapKey = $"{excelRef}|||{excelAccountName}";
                    if (mappingDict.TryGetValue(mapKey, out var customMap))
                    {
                        targetRef = customMap.MappedRef;
                    }

                    var coa = existingCoas.FirstOrDefault(c => c.ReferenceNumber == targetRef);

                    if (coa == null)
                    {
                        continue;
                    }

                    importLines.Add(new GeneralJournalWriter.Line(
                        coa.Id,
                        lineDto.Description,
                        lineDto.Debit ?? 0m,
                        lineDto.Credit ?? 0m));
                }

                if (importLines.Count > 0)
                {
                    int journalEntryId = await GeneralJournalWriter.NextJournalEntryIdAsync(context);

                    context.GeneralJournals.AddRange(GeneralJournalWriter.BuildRows(
                        journalEntryId, userId, transactionNumber, txDto.JournalType,
                        txDate, txDate, null, importLines));
                    importedEntriesCount++;
                }
            }

            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new ImportJournalResultViewModel
            {
                Message = "Journal data successfully imported.",
                ImportedEntriesCount = importedEntriesCount,
            };
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }
}
