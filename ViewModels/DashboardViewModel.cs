using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockFront.Contracts.Dashboard;

namespace stockfront.ViewModels;

/// <summary>
/// The warehouse dashboard: four metric cards and the products table. Reads its data through
/// <see cref="IDashboardRepository"/> and exposes it pre-formatted for the view.
/// </summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private readonly IDashboardRepository _dashboard;

    public DashboardViewModel(IDashboardRepository dashboard) => _dashboard = dashboard;

    [ObservableProperty] private string _totalUnitsText = "—";
    [ObservableProperty] private string _activeOrdersText = "—";
    [ObservableProperty] private string _lowStockText = "—";
    [ObservableProperty] private string _revenueText = "—";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;

    public ObservableCollection<DashboardProductRow> Products { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        Error = null;
        try
        {
            var snapshot = await _dashboard.GetSnapshotAsync();

            TotalUnitsText = snapshot.TotalUnitsInStock.ToString("N0", Ru);
            ActiveOrdersText = snapshot.ActiveOrders.ToString("N0", Ru);
            LowStockText = snapshot.LowStockCount.ToString("N0", Ru);
            RevenueText = FormatRevenue(snapshot.MonthRevenue);

            Products.Clear();
            foreach (var row in snapshot.Products)
                Products.Add(row);
        }
        catch (Exception ex)
        {
            Error = $"Не удалось загрузить данные: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // "412 K" style for thousands, full number below 1000.
    private static string FormatRevenue(decimal value) =>
        value >= 1000m
            ? $"{(value / 1000m).ToString("N0", Ru)} K"
            : value.ToString("N0", Ru);
}
