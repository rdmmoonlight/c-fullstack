// <copyright file="SelectedPeriodHelper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using AumoBackend.Data;
using AumoBackend.Models;
using Microsoft.EntityFrameworkCore;
namespace AumoBackend.Helpers
{
    public static class SelectedPeriodHelper
    {
        // 1. Method Utama yang Dipanggil oleh JournalController
        public static async Task<Period?> GetSelectedPeriodAsync(AppDbContext db, Guid userId)
        {
            if (db == null || userId == Guid.Empty)
            {
                return null;
            }

            return await db.Periods
                .AsNoTracking() // Agar selalu mengambil data fresh dari DB tanpa terhalang cache EF Core
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsSelected);
        }

        // 2. Overload jika menerima parameter bertipe object (Safety Fallback)
        public static async Task<Period?> GetSelectedPeriodAsync(object a1, object a2 = null!)
        {
            if (a1 is AppDbContext db && a2 is Guid userId)
            {
                return await GetSelectedPeriodAsync(db, userId);
            }

            return null;
        }

        // 3. Helper Tambahan untuk Memilih Periode (Dapat dipanggil dari PeriodsController)
        public static async Task SelectPeriodAsync(AppDbContext db, Guid userId, int periodId)
        {
            if (db == null || userId == Guid.Empty)
            {
                return;
            }

            var userPeriods = await db.Periods
                .Where(p => p.UserId == userId)
                .ToListAsync();

            foreach (var period in userPeriods)
            {
                period.IsSelected = period.Id == periodId;
            }

            await db.SaveChangesAsync();
        }

        // 4. Hapus pilihan periode milik user (dipanggil dari PeriodsController dan PeriodsService)
        public static async Task ClearSelectionAsync(AppDbContext db, Guid userId)
        {
            if (db == null || userId == Guid.Empty)
            {
                return;
            }

            await db.Periods
                .Where(p => p.UserId == userId && p.IsSelected)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsSelected, false));
        }

        // Keep stub lain jika diperlukan oleh kompilasi modul lain
        public static Period? GetSelectedPeriod(HttpContext context) => null;

        public static Task<Period?> GetSelectedPeriodAsync(HttpContext context) => Task.FromResult<Period?>(null);

        public static int? GetSelectedPeriodId(HttpContext context) => null;

        public static Task ClearSelectionAsync(HttpContext context) => Task.CompletedTask;
    }
}
