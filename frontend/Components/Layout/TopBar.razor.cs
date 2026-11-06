using AumoFinance.Services;
using Microsoft.AspNetCore.Components;

namespace AumoFinance.Components.Layout
{
    public partial class TopBar
    {
        [Inject]
        private SelectedPeriodProvider PeriodProvider { get; set; } = default!;

        [Inject]
        private UserContext CurrentUser { get; set; } = default!;

        private string? activePeriodName;
        private bool hasActivePeriod;
        private bool isViewingClosed;
        private string periodText = "No Period Selected";
        private string iconClass = "bi-calendar-x text-warning";

        protected override async Task OnInitializedAsync()
        {
            var userId = await CurrentUser.GetUserIdAsync();
            if (userId == Guid.Empty)
            {
                return;
            }

            // Periode aktif ditentukan oleh helper backend (SSOT).
            var active = await PeriodProvider.GetAsync(userId);
            if (active is null)
            {
                return;
            }

            activePeriodName = active.PeriodName;
            hasActivePeriod = !string.IsNullOrEmpty(activePeriodName);
            isViewingClosed = active.IsClosed;
            periodText = hasActivePeriod ? activePeriodName! : "No Period Selected";
            iconClass = !hasActivePeriod
                ? "bi-calendar-x text-warning"
                : (isViewingClosed ? "bi-lock-fill text-secondary" : "bi-calendar2-check text-success");
        }
    }
}
