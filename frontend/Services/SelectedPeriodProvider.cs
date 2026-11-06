using AumoBackend.Data;
using AumoBackend.Helpers;
using AumoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AumoFinance.Services;

/// <summary>
/// Mengambil periode terpilih lewat helper backend (SelectedPeriodHelper) agar aturan pemilihan periode
/// hanya ada di backend. Memakai DbContext factory supaya aman dipakai di sirkuit Blazor Server.
/// </summary>
public sealed class SelectedPeriodProvider
{
    private readonly IDbContextFactory<AppDbContext> factory;

    public SelectedPeriodProvider(IDbContextFactory<AppDbContext> factory) => this.factory = factory;

    public async Task<Period?> GetAsync(Guid userId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await SelectedPeriodHelper.GetSelectedPeriodAsync(db, userId);
    }
}
