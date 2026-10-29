namespace SportHub.ViewModels;

// =====================================================
// STUDENT C REPORTING MODULE - ADMIN DASHBOARD DATA
// Keeps calculated reporting data out of Entity classes.
// =====================================================
public class AdminDashboardViewModel
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int TotalReservations { get; set; }
    public int PendingReservations { get; set; }
    public int ApprovedReservations { get; set; }
    public int CompletedReservations { get; set; }
    public int TotalParticipants { get; set; }
    public int ActiveFacilities { get; set; }
    public int UpcomingMaintenanceBlocks { get; set; }
    public decimal PaidRevenue { get; set; }
    public IReadOnlyList<DashboardChartItemViewModel> StatusBreakdown { get; set; } = [];
    public IReadOnlyList<DashboardChartItemViewModel> PopularFacilities { get; set; } = [];
    public IReadOnlyList<DashboardRevenueItemViewModel> RevenueTrend { get; set; } = [];
}

public class DashboardChartItemViewModel
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DashboardRevenueItemViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
