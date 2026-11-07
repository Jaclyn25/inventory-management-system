using System.ComponentModel.DataAnnotations;
namespace Stockroom.Api.Contracts;

public sealed record ProductInput(
    [Required, RegularExpression("^[A-Za-z0-9][A-Za-z0-9_-]{1,39}$")] string Sku,
    [Required, StringLength(160)] string Name,
    [StringLength(2000)] string Description,
    Guid CategoryId,
    [Range(typeof(decimal), "0", "999999999.99")] decimal UnitPrice,
    [Range(typeof(decimal), "0", "999999999.99")] decimal CostPrice,
    [Range(0, 1000000000)] int LowStockThreshold,
    [Range(1, long.MaxValue)] long Version = 1);

public sealed record ProductView(Guid Id, string Sku, string Name, string Description, Guid CategoryId, string CategoryName,
    decimal UnitPrice, decimal CostPrice, int LowStockThreshold, int Quantity, bool IsArchived, long Version, DateTime CreatedAt);
public sealed record Page<T>(IReadOnlyList<T> Items, int Total, int PageNumber, int PageSize);
public sealed class ProductQuery
{
    [StringLength(160)] public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    [RegularExpression("^(all|low|out|healthy)$")] public string Status { get; set; } = "all";
    public bool Archived { get; set; }
    [RegularExpression("^(name|sku|quantity|value)$")] public string Sort { get; set; } = "name";
    public bool Descending { get; set; }
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
public sealed record VersionInput([Range(1, long.MaxValue)] long Version);
