namespace AumoBackend.DTOs;

using System.Collections.Generic;

public class AccountItemDto
{
    public int Id { get; set; }

    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public decimal Balance { get; set; }
}

public class ChartOfAccountsListResponseDto
{
    public bool Success { get; set; }

    public string? SelectedPeriodName { get; set; }

    public IEnumerable<AccountItemDto> Accounts { get; set; } = new List<AccountItemDto>();
}

public class CreateAccountRequestDto
{
    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? Role { get; set; }
}

public class UpdateAccountRequestDto
{
    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? Role { get; set; }

    public bool IsActive { get; set; }
}

public class ServiceResultDto
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? AccountId { get; set; }

    public int StatusCode { get; set; } = 200;
}

public class AccountDto
{
    public int Id { get; set; }

    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public class CreateAccountRequest
{
    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public class UpdateAccountRequest
{
    public int ReferenceNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
