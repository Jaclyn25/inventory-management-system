namespace Stockroom.Api.Data;

public sealed class StockMovement
{
    public Guid Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string Fingerprint { get; set; } = "";
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Kind { get; set; } = "in";
    public int Delta { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public decimal CostPrice { get; set; }
    public string Reason { get; set; } = "";
    // PostgreSQL stores microseconds; match its precision for byte-stable retry timestamps.
    public DateTime CreatedAt { get; set; } = new(DateTime.UtcNow.Ticks / 10 * 10, DateTimeKind.Utc);
}
