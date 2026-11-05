namespace AumoBackend.DTOs;

public class MarketDataResponse
{
    public bool Success { get; set; }

    public MarketDetail? Usd { get; set; }

    public MarketDetail? Ihsg { get; set; }

    public string? BiRate { get; set; }
}

public class MarketDetail
{
    public double Price { get; set; }

    public double Percent { get; set; }

    public bool IsUp { get; set; }
}

public class JournalImportRequestDto
{
    public int TargetYear { get; set; }

    public int TargetMonth { get; set; }

    public List<JournalTransactionDto> Transactions { get; set; } = new();

    public List<AccountMappingDetailDto>? CustomMappings { get; set; }
}

public class JournalTransactionDto
{
    public string TransactionNumber { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public string JournalType { get; set; } = string.Empty;

    public List<JournalLineDto> Lines { get; set; } = new();
}

public class JournalLineDto
{
    public int RefNumber { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal? Debit { get; set; }

    public decimal? Credit { get; set; }
}

public class AccountMappingDetailDto
{
    public int ExcelRef { get; set; }

    public string ExcelAccountName { get; set; } = string.Empty;

    public int MappedRef { get; set; }

    public string MappedAccountName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
