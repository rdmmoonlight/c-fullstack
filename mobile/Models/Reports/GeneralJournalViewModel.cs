using System;
using System.Collections.Generic;
using System.Globalization;

namespace AumoFinance.Models.Reports;

public class GeneralJournalEntryViewModel
{
    public int Id { get; set; }
    public DateTime EntryDate { get; set; }

    // Timestamp pembuatan asli dari server — dipakai untuk urutan & tampilan jam,
    // karena EntryDate (tanggal transaksi pilihan user) tidak punya komponen waktu asli.
    public DateTime CreatedAt { get; set; }

    // Last time this entry was edited — null as long as it has never been edited.
    public DateTime? UpdatedAt { get; set; }
    public string JournalType { get; set; } = "General";
    public string TransactionNumber { get; set; } = string.Empty;
    public List<GeneralJournalLineViewModel> Lines { get; set; } = new();
    public CultureInfo IdrCulture { get; set; } = new("id-ID");

    public string FormattedDate => EntryDate.ToString("dd MMM yyyy");
    public string FormattedTime => CreatedAt.ToString("dd-MM-yyyy HH:mm:ss");
    public bool HasBeenEdited => UpdatedAt.HasValue;
    public string FormattedEditTime => UpdatedAt?.ToString("dd-MM-yyyy HH:mm:ss") ?? string.Empty;
    public string FormattedTransactionNumber => AumoFinance.Services.TransactionNumberDisplayHelper.Format(TransactionNumber);
}

public class GeneralJournalLineViewModel
{
    public int AccountReferenceNumber { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string? LineDescription { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public CultureInfo IdrCulture { get; set; } = new("id-ID");

    // Baris kredit ditandai supaya nama akunnya bisa di-indent (satu tab) di UI.
    public bool IsCredit => Credit > 0;

    // Rupiah, tanpa desimal/koma — mis. "Rp1.500.000".
    public string FormattedDebit => Debit > 0 ? Debit.ToString("C0", IdrCulture) : "-";
    public string FormattedCredit => Credit > 0 ? Credit.ToString("C0", IdrCulture) : "-";

    public bool HasDescription => !string.IsNullOrWhiteSpace(LineDescription);
    public string LineDescriptionDisplay => HasDescription ? LineDescription!.ToLowerInvariant() : string.Empty;
}

// Grup entri jurnal per tanggal (dipakai CollectionView.IsGrouped) — entri dengan
// tanggal sama dikelompokkan jadi satu header, tapi urutan di dalamnya tetap
// berdasarkan jam:menit:detik (paling lama dulu).
public class GeneralJournalDateGroup : List<GeneralJournalEntryViewModel>
{
    public string GroupHeader { get; }

    public GeneralJournalDateGroup(string groupHeader, IEnumerable<GeneralJournalEntryViewModel> items) : base(items)
    {
        GroupHeader = groupHeader;
    }
}
