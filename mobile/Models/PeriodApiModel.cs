using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AumoFinance.Models;

public class PeriodApiModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("periodName")]
    public string PeriodName { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("isClosed")]
    public bool IsClosed { get; set; }

    [JsonPropertyName("isSelected")]
    public bool IsSelected { get; set; }

    public string DateRangeDisplay => $"{StartDate:dd MMM yyyy} - {EndDate:dd MMM yyyy}";
    public bool CanSelect => !IsSelected;
    public bool CanClose => !IsClosed;

    // Periode closed tetap bisa "dibuka" untuk dilihat datanya (jadi periode yang
    // sedang di-view di seluruh aplikasi), tapi tidak bisa diedit lagi — jadi
    // labelnya "View", bukan "Select Period", supaya jelas ini bukan aksi transaksi.
    public string SelectButtonText => IsClosed ? "View" : "Select Period";
}

// Wrapper respons GET /api/mobile/periods: { success, selectedPeriodId, periods: [...] }
public class PeriodsEnvelopeModel
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("selectedPeriodId")]
    public int? SelectedPeriodId { get; set; }

    [JsonPropertyName("periods")]
    public List<PeriodApiModel> Periods { get; set; } = new();
}
