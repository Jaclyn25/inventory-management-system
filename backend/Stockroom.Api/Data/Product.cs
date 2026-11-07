namespace Stockroom.Api.Data;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int LowStockThreshold { get; set; }
    public int Quantity { get; set; }
    public bool IsArchived { get; set; }
    public long Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
