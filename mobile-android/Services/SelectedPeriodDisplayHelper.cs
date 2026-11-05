using System.Linq;
using System.Threading.Tasks;
using AumoFinance.Views;

namespace AumoFinance.Services;

/// <summary>
/// Mengisi TopBarView.PeriodText dengan periode yang sedang di-select/di-view,
/// supaya halaman-halaman lain (Dashboard, semua Reports) tidak perlu
/// menampilkan indikator periode versi mereka sendiri lagi — cukup satu
/// sumber kebenaran di top bar.
/// </summary>
public static class SelectedPeriodDisplayHelper
{
    public static async Task ApplyToTopBarAsync(TopBarView topBar, PeriodService periodService)
    {
        var (periods, selectedPeriodId, _) = await periodService.GetPeriodsAsync();

        if (periods == null || periods.Count == 0)
        {
            topBar.PeriodText = "No Period";
            return;
        }

        int? selectedId = int.TryParse(selectedPeriodId, out var parsedId) ? parsedId : null;
        var selected = selectedId.HasValue ? periods.FirstOrDefault(p => p.Id == selectedId.Value) : null;

        topBar.PeriodText = selected?.PeriodName ?? "No Active Period";
    }
}
