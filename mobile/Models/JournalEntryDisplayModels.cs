using System.Globalization;

namespace AumoFinance.Models;

public class JournalEntryDisplayModel
{
    public int Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }

    // Timestamp asli pembuatan entry di server — dipakai untuk tampilan
    // tanggal+jam input yang sesungguhnya, karena EntryDate cuma tanggal
    // pilihan user dari DatePicker (jam selalu 00:00:00).
    public DateTime CreatedAt { get; set; }

    // Last time this entry was edited — null as long as it has never been edited.
    public DateTime? UpdatedAt { get; set; }
    public List<JournalEntryLineDisplayModel> Lines { get; set; } = new();

    public string FormattedTime => CreatedAt.ToString("dd-MM-yyyy HH:mm:ss");
    public bool HasBeenEdited => UpdatedAt.HasValue;
    public string FormattedEditTime => UpdatedAt?.ToString("dd-MM-yyyy HH:mm:ss") ?? string.Empty;
    public string FormattedTransactionNumber => AumoFinance.Services.TransactionNumberDisplayHelper.Format(TransactionNumber);
}

public class JournalEntryLineDisplayModel
{
    public string AccountName { get; set; } = string.Empty;
    public string RefNumber { get; set; } = string.Empty;
    public string? LineDescription { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public CultureInfo IdrCulture { get; set; } = new("id-ID");

    // Baris kredit ditandai supaya nama akunnya bisa di-indent (satu tab) di UI.
    public bool IsCredit => Credit > 0;
    public bool HasDescription => !string.IsNullOrWhiteSpace(LineDescription);
    public string LineDescriptionDisplay => HasDescription ? LineDescription!.ToLowerInvariant() : string.Empty;

    // Rupiah, tanpa desimal/koma — mis. "Rp 1.500.000".
    public string FormattedDebit => Debit > 0 ? Debit.ToString("C0", IdrCulture) : "-";
    public string FormattedCredit => Credit > 0 ? Credit.ToString("C0", IdrCulture) : "-";
}

// Grup entri jurnal per tanggal (dipakai CollectionView.IsGrouped), pola sama
// dengan GeneralJournalDateGroup supaya tampilannya konsisten dengan General Journal.
public class JournalEntryDateGroup : List<JournalEntryDisplayModel>
{
    public string GroupHeader { get; }

    public JournalEntryDateGroup(string groupHeader, IEnumerable<JournalEntryDisplayModel> items) : base(items)
    {
        GroupHeader = groupHeader;
    }
}
