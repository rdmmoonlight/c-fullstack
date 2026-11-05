namespace AumoBackend.Models
{
    public enum AccountClassification
    {
        Asset,
        Liability,
        Equity,
        Revenue,
        Expense,
    }

    public static class AccountClassificationExtensions
    {
        public static bool NormalBalanceIsDebit(this AccountClassification classification)
        {
            return classification == AccountClassification.Asset
                || classification == AccountClassification.Expense;
        }

        public static bool IsTemporary(this AccountClassification classification)
        {
            return classification == AccountClassification.Revenue
                || classification == AccountClassification.Expense;
        }

        public static bool IsPermanent(this AccountClassification classification)
        {
            return !IsTemporary(classification);
        }
    }

    public static class AccountClassificationHelper
    {
        public static bool NormalBalanceIsDebit(AccountClassification classification)
            => classification.NormalBalanceIsDebit();

        // Saldo Normal:
        // Debit  : Assets, OperatingExpenses, OtherExpenses
        // Kredit : Liabilities, Equity, OperatingIncome, OtherIncome
        public static bool NormalBalanceIsDebit(string? accountType)
        {
            return accountType switch
            {
                "Assets" or "OperatingExpenses" or "OtherExpenses" => true,
                "Liabilities" or "Equity" or "OperatingIncome" or "OtherIncome" => false,
                _ => true,
            };
        }

        public static bool IsTemporary(AccountClassification classification)
            => classification.IsTemporary();

        public static bool IsPermanent(AccountClassification classification)
            => classification.IsPermanent();

        public static bool ValidateReferenceNumber(string? refNum)
        {
            if (string.IsNullOrWhiteSpace(refNum))
            {
                return false;
            }

            return int.TryParse(refNum, out _);
        }

        // Akun temporer = akun laba rugi (pendapatan dan beban); ditutup ke Laba Ditahan saat closing.
        // Akun permanen = akun posisi keuangan (aset, liabilitas, ekuitas).
        internal static bool IsTemporary(string type)
        {
            return type is "OperatingIncome" or "OtherIncome" or "OperatingExpenses" or "OtherExpenses";
        }

        internal static bool IsPermanent(string type)
        {
            return !IsTemporary(type);
        }

        private static readonly Dictionary<string, (int Start, int End)> ReferenceRanges = new()
        {
            ["Assets"] = (100, 199),
            ["Liabilities"] = (200, 299),
            ["Equity"] = (300, 399),
            ["OperatingIncome"] = (400, 499),
            ["OperatingExpenses"] = (500, 599),
            ["OtherIncome"] = (600, 799),
            ["OtherExpenses"] = (800, 999),
        };

        public static IReadOnlyCollection<string> AccountTypes => ReferenceRanges.Keys;

        public static readonly string[] AccountRoles = { "Default", "CashAndEquivalents", "RetainedEarnings" };

        public static bool IsKnownAccountType(string? type)
            => type != null && ReferenceRanges.ContainsKey(type);

        internal static bool ValidateReferenceNumber(string type, int referenceNumber)
        {
            return ReferenceRanges.TryGetValue(type ?? string.Empty, out var range)
                && referenceNumber >= range.Start
                && referenceNumber <= range.End;
        }
    }
}
