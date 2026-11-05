namespace AumoBackend.DTOs;

using System;
using System.Collections.Generic;

public class CreatePeriodRequest
{
    public const string ModeLoadExisting = "LoadExisting";
    public const string ModeCreateNew = "CreateNew";

    public int Month { get; set; }

    public int Year { get; set; }

    public string SetupMode { get; set; } = string.Empty;

    // Properties for ModeLoadExisting
    public int? CashAccountId { get; set; }

    public int? BankAccountId { get; set; }

    public int? RetainedEarningsAccountId { get; set; }

    // Properties for ModeCreateNew
    public string? CashAccountCode { get; set; }

    public string? CashAccountName { get; set; }

    public decimal? CashBalance { get; set; }

    public string? BankAccountCode { get; set; }

    public string? BankAccountName { get; set; }

    public decimal? BankBalance { get; set; }

    public string? RetainedEarningsAccountCode { get; set; }

    public string? RetainedEarningsAccountName { get; set; }
}

public class PeriodDto
{
    public int Id { get; set; }

    public string PeriodName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsClosed { get; set; }

    public bool IsSelected { get; set; }
}

public class GetPeriodsResponse
{
    public bool Success { get; set; }

    public int? SelectedPeriodId { get; set; }

    public List<PeriodDto> Periods { get; set; } = new();
}

public class AccountSimpleDto
{
    public int Id { get; set; }

    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string DisplayLabel { get; set; } = string.Empty;
}

public class OpenPeriodInfoResponse
{
    public bool Success { get; set; }

    public bool HasExistingPermanentAccounts { get; set; }

    public List<AccountSimpleDto> AvailableCashAndBankAccounts { get; set; } = new();

    public List<AccountSimpleDto> AvailableRetainedEarningsAccounts { get; set; } = new();

    public List<AccountSimpleDto> PermanentAccounts { get; set; } = new();
}

public class CreatePeriodResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? PeriodId { get; set; }

    // true bila kegagalan berasal dari server (bukan kesalahan input pengguna) -> HTTP 500.
    public bool IsServerError { get; set; }
}

public class SelectPeriodResult
{
    public bool Success { get; set; }

    public int SelectedPeriodId { get; set; }

    public string Message { get; set; } = string.Empty;
}

public class BaseServiceResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}
