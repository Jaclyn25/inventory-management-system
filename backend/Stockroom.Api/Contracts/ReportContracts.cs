namespace Stockroom.Api.Contracts;

public sealed record CategoryDistribution(Guid CategoryId, string Name, int Products, long Units, decimal Value);
public sealed record DailyActivity(DateOnly Date, long Received, long Issued, long Adjusted);
public sealed record InventoryReport(int TotalProducts, long TotalUnits, decimal InventoryValue, decimal RetailValue,
    int LowStockProducts, int OutOfStockProducts, IReadOnlyList<CategoryDistribution> Categories,
    IReadOnlyList<ProductView> AttentionProducts, IReadOnlyList<MovementView> RecentMovements,
    IReadOnlyList<DailyActivity> Activity, DateTime AsOf, string Currency = "USD");
