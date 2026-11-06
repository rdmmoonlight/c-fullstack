using System.Globalization;
using AumoBackend.DTOs;
using AumoBackend.Services.Tools;
using AumoBackend.ViewModels;
using ClosedXML.Excel;

namespace AumoFinance.Services;

public sealed class ImportPreviewLine
{
    public int RowIndex { get; set; }
    public int RefNumber { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    public bool IsNewAccount { get; set; }
}

public sealed class ImportPreviewTransaction
{
    public string JournalType { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<ImportPreviewLine> Lines { get; set; } = new();
}

/// <summary>Hasil pratinjau impor untuk ditampilkan UI. Permintaan asli disimpan untuk dikirim ke backend saat konfirmasi.</summary>
public sealed class JournalImportPreview
{
    public JournalImportRequestDto Request { get; set; } = new();
    public int TotalTransactionsRead => Transactions.Count;
    public int TotalLinesRead => Transactions.Sum(t => t.Lines.Count);
    public List<string> Warnings { get; set; } = new();
    public List<ImportPreviewTransaction> Transactions { get; set; } = new();

    /// <summary>Alias untuk halaman yang memakai nama JournalEntries.</summary>
    public List<ImportPreviewTransaction> JournalEntries => Transactions;
}

/// <summary>
/// Alur impor jurnal untuk UI. Frontend hanya membaca berkas Excel menjadi permintaan;
/// pencocokan akun, penomoran, pembuatan periode, dan penyimpanan dilakukan IToolsService (backend).
/// </summary>
public sealed class JournalImportWorkflow
{
    private static readonly (string Sheet, string JournalType)[] Sheets = { ("GJ", "General"), ("AJ", "Adjusting") };

    private readonly IToolsService tools;
    private readonly SelectedPeriodProvider periods;

    public JournalImportWorkflow(IToolsService tools, SelectedPeriodProvider periods)
    {
        this.tools = tools;
        this.periods = periods;
    }

    public async Task<(JournalImportPreview? Preview, string? Error)> BuildPreviewAsync(Guid userId, Stream xlsx)
    {
        var warnings = new List<string>();
        var transactions = ParseWorkbook(xlsx, warnings);
        if (transactions.Count == 0)
        {
            return (null, "No valid transactions found in 'GJ' or 'AJ' sheets. Please verify your template format.");
        }

        var request = new JournalImportRequestDto { Transactions = transactions };

        // Bulan/tahun tujuan: periode yang sedang dipilih, atau tanggal transaksi pertama.
        var selected = await periods.GetAsync(userId);
        if (selected is not null)
        {
            request.TargetYear = selected.StartDate.Year;
            request.TargetMonth = selected.StartDate.Month;
        }
        else if (DateTime.TryParse(transactions[0].Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
        {
            request.TargetYear = first.Year;
            request.TargetMonth = first.Month;
        }

        var preview = await tools.PreviewJournalImportAsync(userId, request);
        var unmapped = preview.AccountMappings
            .Where(m => m.Status == "UNMAPPED" || m.MappedRef == 0)
            .Select(m => $"{m.ExcelRef}|{m.ExcelAccountName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new JournalImportPreview { Request = request, Warnings = warnings };
        foreach (var tx in preview.Transactions)
        {
            _ = DateTime.TryParse(tx.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
            var view = new ImportPreviewTransaction { JournalType = tx.JournalType, Date = date };
            var row = 0;
            foreach (var line in tx.Lines)
            {
                row++;
                view.Lines.Add(new ImportPreviewLine
                {
                    RowIndex = row,
                    RefNumber = line.RefNumber,
                    AccountName = line.AccountName,
                    Description = line.Description,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    IsNewAccount = unmapped.Contains($"{line.RefNumber}|{line.AccountName}"),
                });
            }

            result.Transactions.Add(view);
        }

        if (preview.Summary.UnmappedCount > 0)
        {
            warnings.Add($"{preview.Summary.UnmappedCount} account(s) are not in your Chart of Accounts yet.");
        }

        return (result, null);
    }

    public async Task<ImportJournalResultViewModel> ImportAsync(Guid userId, JournalImportPreview preview)
        => await tools.ImportJournalEntriesAsync(userId, preview.Request);

    /// <summary>Aturan template: baris dengan Date yang sama pada satu sheet menjadi satu entri jurnal.</summary>
    private static List<JournalTransactionDto> ParseWorkbook(Stream xlsx, List<string> warnings)
    {
        var result = new List<JournalTransactionDto>();
        using var workbook = new XLWorkbook(xlsx);

        foreach (var (sheetName, journalType) in Sheets)
        {
            var sheet = workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));
            if (sheet is null)
            {
                warnings.Add($"Worksheet '{sheetName}' was not found.");
                continue;
            }

            var byDate = new Dictionary<DateTime, JournalTransactionDto>();
            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var dateCell = row.Cell(1);
                DateTime date;
                if (dateCell.DataType == XLDataType.DateTime)
                {
                    date = dateCell.GetDateTime();
                }
                else if (!DateTime.TryParse(dateCell.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    warnings.Add($"{sheetName} row {row.RowNumber()}: invalid date, row skipped.");
                    continue;
                }

                _ = int.TryParse(row.Cell(4).GetString().Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var refNumber);
                var line = new JournalLineDto
                {
                    RefNumber = refNumber,
                    AccountName = row.Cell(2).GetString().Trim(),
                    Description = row.Cell(3).GetString().Trim(),
                    Debit = ReadAmount(row.Cell(5)),
                    Credit = ReadAmount(row.Cell(6)),
                };

                var key = date.Date;
                if (!byDate.TryGetValue(key, out var tx))
                {
                    tx = new JournalTransactionDto
                    {
                        Date = key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        JournalType = journalType,
                    };
                    byDate[key] = tx;
                    result.Add(tx);
                }

                tx.Lines.Add(line);
            }
        }

        return result;
    }

    private static decimal? ReadAmount(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue<decimal>(out var value))
        {
            return value == 0 ? null : value;
        }

        return decimal.TryParse(cell.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) && parsed != 0
            ? parsed
            : null;
    }
}
