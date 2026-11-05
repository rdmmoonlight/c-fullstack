// <copyright file="JournalEntryController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace AumoBackend.Controllers;

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AumoBackend.Data;
using AumoBackend.DTOs;
using AumoBackend.Models;
using AumoBackend.Services.JournalEntries;
using AumoBackend.Validators.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("/journal-entry")]
[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]

public class JournalEntryController : ControllerBase
{
    private readonly AppDbContext db;
    private readonly ITransactionNumberService transactionNumberService;
    private readonly IValidator<CreateJournalEntryRequest> createValidator;
    private readonly IValidator<UpdateJournalEntryRequest> updateValidator;
    private readonly IValidator<SearchDescriptionsQuery> searchValidator;
    private readonly IValidator<NextTransactionNumberQuery> nextNumberValidator;

    public JournalEntryController(
        AppDbContext db,
        ITransactionNumberService transactionNumberService,
        IValidator<CreateJournalEntryRequest> createValidator,
        IValidator<UpdateJournalEntryRequest> updateValidator,
        IValidator<SearchDescriptionsQuery> searchValidator,
        IValidator<NextTransactionNumberQuery> nextNumberValidator)
    {
        this.db = db;
        this.transactionNumberService = transactionNumberService;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
        this.searchValidator = searchValidator;
        this.nextNumberValidator = nextNumberValidator;
    }

    // ==========================================
    // 1. GET: /journal-entry/{id}
    // ==========================================
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        // id = JournalEntryId (id kelompok jurnal)
        var rows = await this.db.GeneralJournals
            .AsNoTracking()
            .Where(g => g.JournalEntryId == id && g.UserId == userId)
            .OrderBy(g => g.LineOrder)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return this.NotFound(new { success = false, message = "Journal entry not found." });
        }

        var entry = rows[0];

        var closedPeriods = await this.db.Periods
            .Where(p => p.UserId == userId && p.IsClosed)
            .ToListAsync();

        bool isLocked = PeriodLock.IsDateLocked(entry.EntryDate, closedPeriods);

        var result = new
        {
            Id = entry.JournalEntryId,
            entry.TransactionNumber,
            entry.JournalType,
            entry.EntryDate,
            entry.CreatedAt,
            entry.UpdatedAt,
            isLocked,
            lines = rows.Select(l => new
            {
                Id = l.LineId,
                l.AccountId,
                l.LineDescription,
                l.Debit,
                l.Credit,
                l.LineOrder
            }),
        };

        return this.Ok(new { success = true, entry = result });
    }

    // ==========================================
    // 2. POST: /journal-entry/create
    // ==========================================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateJournalEntryRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        var validation = await this.createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var effectiveLines = request.Lines
            .Where(l => l.AccountId != 0 && (l.Debit != 0 || l.Credit != 0))
            .ToList();

        var validAccountIds = (await this.db.ChartOfAccounts
            .Where(a => a.IsActive && a.UserId == userId)
            .Select(a => a.Id)
            .ToListAsync())
            .ToHashSet();

        if (effectiveLines.Any(l => !validAccountIds.Contains(l.AccountId)))
        {
            return this.BadRequest(new { success = false, message = "One or more selected accounts are invalid or inactive." });
        }

        var closedPeriods = await this.db.Periods
            .Where(p => p.UserId == userId && p.IsClosed)
            .ToListAsync();

        if (PeriodLock.IsDateLocked(request.EntryDate, closedPeriods))
        {
            return this.BadRequest(new { success = false, message = "This date falls within a closed accounting period. Choose a date in an open period." });
        }

        string journalType = string.IsNullOrWhiteSpace(request.JournalType) ? "General" : request.JournalType;
        string transactionNumber = await this.transactionNumberService.GenerateAsync(userId, journalType, request.EntryDate);

        var deviceCreatedAt = request.CreatedAt == default ? DateTime.UtcNow : request.CreatedAt;
        var entryDate = DateTime.SpecifyKind(request.EntryDate, DateTimeKind.Utc);
        var createdAt = DateTime.SpecifyKind(deviceCreatedAt, DateTimeKind.Utc);

        int journalEntryId = await GeneralJournalWriter.NextJournalEntryIdAsync(this.db);

        var rows = GeneralJournalWriter.BuildRows(
            journalEntryId, userId, transactionNumber, journalType, entryDate, createdAt, null,
            effectiveLines.Select(l => new GeneralJournalWriter.Line(l.AccountId, l.LineDescription, l.Debit, l.Credit)));

        this.db.GeneralJournals.AddRange(rows);
        await this.db.SaveChangesAsync();

        return this.Ok(new
        {
            success = true,
            message = $"Journal entry {transactionNumber} has been posted.",
            entryId = journalEntryId,
            transactionNumber,
        });
    }

    // ==========================================
    // 3. PUT: /journal-entry/edit/{id}
    // ==========================================
    [HttpPut("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, [FromBody] UpdateJournalEntryRequest request)
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        var validation = await this.updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        // id = JournalEntryId (id kelompok jurnal)
        var existingRows = await this.db.GeneralJournals
            .Where(g => g.JournalEntryId == id && g.UserId == userId)
            .ToListAsync();

        if (existingRows.Count == 0)
        {
            return this.NotFound(new { success = false, message = "Journal entry not found." });
        }

        var header = existingRows[0];

        var closedPeriods = await this.db.Periods
            .Where(p => p.UserId == userId && p.IsClosed)
            .ToListAsync();

        if (PeriodLock.IsDateLocked(header.EntryDate, closedPeriods) || PeriodLock.IsDateLocked(request.EntryDate, closedPeriods))
        {
            return this.BadRequest(new { success = false, message = $"Journal entry {header.TransactionNumber} falls within a closed period and cannot be modified." });
        }

        var effectiveLines = request.Lines
            .Where(l => l.AccountId != 0 && (l.Debit != 0 || l.Credit != 0))
            .ToList();

        var validAccountIds = (await this.db.ChartOfAccounts
            .Where(a => a.IsActive && a.UserId == userId)
            .Select(a => a.Id)
            .ToListAsync())
            .ToHashSet();

        if (effectiveLines.Any(l => !validAccountIds.Contains(l.AccountId)))
        {
            return this.BadRequest(new { success = false, message = "One or more selected accounts are invalid or inactive." });
        }

        string journalType = string.IsNullOrWhiteSpace(request.JournalType) ? header.JournalType : request.JournalType;
        var entryDate = DateTime.SpecifyKind(request.EntryDate, DateTimeKind.Utc);
        var deviceUpdatedAt = request.UpdatedAt == default ? DateTime.UtcNow : request.UpdatedAt;
        var updatedAt = DateTime.SpecifyKind(deviceUpdatedAt, DateTimeKind.Utc);

        var newRows = GeneralJournalWriter.BuildRows(
            header.JournalEntryId, userId, header.TransactionNumber, journalType, entryDate,
            header.CreatedAt, updatedAt,
            effectiveLines.Select(l => new GeneralJournalWriter.Line(l.AccountId, l.LineDescription, l.Debit, l.Credit)),
            header.Status ?? GeneralJournalWriter.PostedStatus);

        // Hapus baris lama + tambah baris baru dalam satu SaveChanges (satu transaksi).
        this.db.GeneralJournals.RemoveRange(existingRows);
        this.db.GeneralJournals.AddRange(newRows);
        await this.db.SaveChangesAsync();

        return this.Ok(new
        {
            success = true,
            message = $"Journal entry {header.TransactionNumber} updated successfully.",
        });
    }

    // ==========================================
    // 4. DELETE: /journal-entry/delete/{id}
    // ==========================================
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        // id = JournalEntryId (id kelompok jurnal)
        var rows = await this.db.GeneralJournals
            .Where(g => g.JournalEntryId == id && g.UserId == userId)
            .ToListAsync();

        if (rows.Count == 0)
        {
            return this.NotFound(new { success = false, message = "Journal entry not found." });
        }

        var closedPeriods = await this.db.Periods
            .Where(p => p.UserId == userId && p.IsClosed)
            .ToListAsync();

        if (PeriodLock.IsDateLocked(rows[0].EntryDate, closedPeriods))
        {
            return this.BadRequest(new { success = false, message = "Cannot delete transactions in closed accounting periods." });
        }

        string transactionNumber = rows[0].TransactionNumber;

        this.db.GeneralJournals.RemoveRange(rows);
        await this.db.SaveChangesAsync();

        return this.Ok(new
        {
            success = true,
            message = $"Journal entry {transactionNumber} deleted successfully.",
        });
    }

    // ==========================================
    // 5. GET: /journal-entry/search-descriptions
    // ==========================================
    [HttpGet("search-descriptions")]
    public async Task<IActionResult> SearchDescriptions([FromQuery] SearchDescriptionsQuery request)
    {
        var validation = await this.searchValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var q = request.Q ?? string.Empty;

        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return this.Ok(Array.Empty<string>());
        }

        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        var keyword = q.Trim();

        var results = await this.db.GeneralJournals
            .Where(l => l.UserId == userId
                     && l.LineDescription != null && l.LineDescription != string.Empty
                     && EF.Functions.ILike(l.LineDescription, $"%{keyword}%"))
            .GroupBy(l => l.LineDescription)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Max(l => l.LineId))
            .Select(g => g.Key)
            .Take(8)
            .ToListAsync();

        return this.Ok(results);
    }

    // ==========================================
    // 6. GET: /journal-entry/next-transaction-number
    // ==========================================
    [HttpGet("next-transaction-number")]
    public async Task<IActionResult> GetNextTransactionNumber([FromQuery] NextTransactionNumberQuery request)
    {
        var validation = await this.nextNumberValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return this.BadRequest(validation.ToErrorResponse());
        }

        var journalType = request.JournalType ?? "General";
        var entryDate = request.EntryDate;

        var userId = this.GetCurrentUserId();
        if (userId == Guid.Empty)
        {
            return this.Unauthorized(new { success = false, message = "User identity is invalid or expired." });
        }

        string type = string.IsNullOrWhiteSpace(journalType) ? "General" : journalType;

        string nextNumber = await this.transactionNumberService.PeekNextAsync(userId, type, entryDate ?? DateTime.Today);

        return this.Ok(new { success = true, transactionNumber = nextNumber });
    }

    private Guid GetCurrentUserId()
    {
        var userIdStr = this.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? this.User.FindFirstValue("sub");

        return Guid.TryParse(userIdStr, out Guid userId) ? userId : Guid.Empty;
    }
}
