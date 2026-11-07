using System.ComponentModel.DataAnnotations;
namespace Stockroom.Api.Contracts;

public sealed record StockInput(Guid OperationId, Guid ProductId,
    [Range(1, 1000000000)] int Quantity,
    [Required, StringLength(500, MinimumLength = 3)] string Reason);
public sealed record MovementView(Guid Id, Guid ProductId, string Sku, string ProductName, string Kind,
    int Delta, int QuantityBefore, int QuantityAfter, decimal CostPrice, string Reason, DateTime CreatedAt);
public sealed class MovementQuery
{
    public Guid? ProductId { get; set; }
    [StringLength(160)] public string? Search { get; set; }
    [RegularExpression("^(in|out|adjustment)$")] public string? Kind { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
}
