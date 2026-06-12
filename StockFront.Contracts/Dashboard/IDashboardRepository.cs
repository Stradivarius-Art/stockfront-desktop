namespace StockFront.Contracts.Dashboard;

/// <summary>
/// Read-only aggregate queries for the warehouse dashboard. Lives in the data layer; the dashboard
/// ViewModel depends only on this interface. Read-only and asynchronous (never blocks the UI thread).
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Build the dashboard snapshot (metrics + product rows) in a single read.</summary>
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken ct = default);
}