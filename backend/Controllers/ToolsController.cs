// <copyright file="ToolsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Controllers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AumoBackend.Data;
using AumoBackend.DTOs;
using AumoBackend.Models;
using AumoBackend.Services.JournalEntries;
using AumoBackend.Validators.Common;
using ClosedXML.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("/tools")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]

public class ToolsController : ControllerBase
{
    private readonly AppDbContext context;
    private readonly IValidator<JournalImportRequestDto> importValidator;

    public ToolsController(AppDbContext context, IValidator<JournalImportRequestDto> importValidator)
    {
        this.context = context;
        this.importValidator = importValidator;
    }

    // ==========================================
    // 1. GET: /tools/download-journal-template
    // ==========================================
    [HttpGet("download-journal-template")]
    public IActionResult DownloadJournalTemplate()
    {
        using var workbook = new XLWorkbook();

        var wsGj = workbook.Worksheets.Add("GJ");
        wsGj.Cell(1, 1).Value = "Date";
        wsGj.Cell(1, 2).Value = "Account Name";
        wsGj.Cell(1, 3).Value = "Description";
        wsGj.Cell(1, 4).Value = "Ref";
        wsGj.Cell(1, 5).Value = "Debit";
        wsGj.Cell(1, 6).Value = "Credit";
        wsGj.Row(1).Style.Font.Bold = true;

        var wsAj = workbook.Worksheets.Add("AJ");
        wsAj.Cell(1, 1).Value = "Date";
        wsAj.Cell(1, 2).Value = "Account Name";
        wsAj.Cell(1, 3).Value = "Description";
        wsAj.Cell(1, 4).Value = "Ref";
        wsAj.Cell(1, 5).Value = "Debit";
        wsAj.Cell(1, 6).Value = "Credit";
        wsAj.Row(1).Style.Font.Bold = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return this.File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "JournalImportTemplate.xlsx");
    }

    // ==========================================
    // 2. POST: /tools/preview-journal-import
    // ==========================================
    [HttpPost("preview-journal-import")]
    public async Task<IActionResult> PreviewJournalImport([FromBody] JournalImportRequestDto request)
    {
        var validation = await this.importValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return this.Unauthorized();
        }

        var existingCoas = await this.context.ChartOfAccounts
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

            // Paksa Tahun & Bulan mengikuti Target
            int day = Math.Min(rawTxDate.Day, DateTime.DaysInMonth(request.TargetYear, request.TargetMonth));
            var txDate = DateTime.SpecifyKind(new DateTime(request.TargetYear, request.TargetMonth, day), DateTimeKind.Utc);

            string prefix = txDto.JournalType.Equals("Adjusting", StringComparison.OrdinalIgnoreCase) ? "AJ" : "GJ";
            string counterKey = $"{prefix}{txDate:yyMM}";

            if (!counterMemory.ContainsKey(counterKey))
            {
                var existingCounter = await this.context.TransactionCounters
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

        return this.Ok(new
        {
            transactions = processedTransactions,
            accountMappings = uniqueMappings,
            summary = new
            {
                totalUniqueAccounts = uniqueMappings.Count,
                exactMatchCount = exactMatchCount,
                reallocatedCount = reallocatedCount,
                unmappedCount = unmappedCount,
                isPerfectMatch = unmappedCount == 0
            },
        });
    }

    // ==========================================
    // 3. POST: /tools/import-journal-entries
    // ==========================================
    [HttpPost("import-journal-entries")]
    public async Task<IActionResult> ImportJournalEntries([FromBody] JournalImportRequestDto request)
    {
        var validation = await this.importValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return this.Unauthorized();
        }

        using var dbTransaction = await this.context.Database.BeginTransactionAsync();

        try
        {
            // Cek / Buat Periode Akuntansi
            var period = await this.context.Periods.FirstOrDefaultAsync(p =>
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
                this.context.Periods.Add(period);
                await this.context.SaveChangesAsync();
            }

            var existingCoas = await this.context.ChartOfAccounts
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync();

            var mappingDict = request.CustomMappings?
                .Where(m => m.MappedRef > 0)
                .ToDictionary(
                    m => $"{m.ExcelRef}|||{m.ExcelAccountName.Trim()}",
                    m => m,
                    StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, AccountMappingDetailDto>();

            var existingTxNumbers = await this.context.GeneralJournals
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

                // PAKSA TANGGAL MENGGUNAKAN TARGET MONTH & TARGET YEAR
                int day = Math.Min(rawTxDate.Day, DateTime.DaysInMonth(request.TargetYear, request.TargetMonth));
                var txDate = DateTime.SpecifyKind(new DateTime(request.TargetYear, request.TargetMonth, day), DateTimeKind.Utc);

                string prefix = txDto.JournalType.Equals("Adjusting", StringComparison.OrdinalIgnoreCase) ? "AJ" : "GJ";
                string counterKey = $"{prefix}{txDate:yyMM}";

                if (!activeCounters.TryGetValue(counterKey, out var counter))
                {
                    counter = await this.context.TransactionCounters.FirstOrDefaultAsync(c =>
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
                        this.context.TransactionCounters.Add(counter);
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
                    int journalEntryId = await GeneralJournalWriter.NextJournalEntryIdAsync(this.context);

                    this.context.GeneralJournals.AddRange(GeneralJournalWriter.BuildRows(
                        journalEntryId, userId, transactionNumber, txDto.JournalType,
                        txDate, txDate, null, importLines));
                    importedEntriesCount++;
                }
            }

            await this.context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return this.Ok(new
            {
                message = "Journal data successfully imported.",
                importedEntriesCount = importedEntriesCount,
            });
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return this.StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Failed to save data: {ex.Message}" });
        }
    }
}
