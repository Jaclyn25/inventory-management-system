using System.Data;
using Microsoft.EntityFrameworkCore;
using Stockroom.Api.Contracts;
using Stockroom.Api.Data;
namespace Stockroom.Api.Services;

public sealed class ReportService(InventoryDb db)
{
    public async Task<InventoryReport> Summary(ProductQuery query)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var asOf = DateTime.UtcNow;
        var products = ProductService.Filter(db.Products.AsNoTracking(), query);
        var total = await products.CountAsync();
        var units = await products.SumAsync(x => (long)x.Quantity);
        var value = await products.SumAsync(x => x.CostPrice * x.Quantity);
        var retail = await products.SumAsync(x => x.UnitPrice * x.Quantity);
        var low = await products.CountAsync(x => x.Quantity > 0 && x.Quantity <= x.LowStockThreshold);
        var outOfStock = await products.CountAsync(x => x.Quantity == 0);
        var categoryRows = await products.GroupBy(x => new { x.CategoryId, x.Category.Name })
            .Select(g => new { g.Key.CategoryId, g.Key.Name, Products = g.Count(), Units = g.Sum(x => (long)x.Quantity), Value = g.Sum(x => x.Quantity * x.CostPrice) })
            .OrderByDescending(x => x.Value).ThenBy(x => x.Name).ToListAsync();
        var categories = categoryRows.Select(x => new CategoryDistribution(x.CategoryId, x.Name, x.Products, x.Units, x.Value)).ToList();
        var attention = await ProductService.Views(products.Where(x => x.Quantity <= x.LowStockThreshold)
            .OrderBy(x => x.Quantity).ThenBy(x => x.Name).Take(8)).ToListAsync();
        var movements = db.Movements.AsNoTracking().Where(m => products.Select(p => p.Id).Contains(m.ProductId));
        var recent = await StockService.Views(movements.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Take(8)).ToListAsync();
        var start = asOf.Date.AddDays(-13);
        var grouped = await movements.Where(x => x.CreatedAt >= start).GroupBy(x => x.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Received = g.Sum(x => x.Kind == "in" ? (long)x.Delta : 0),
                Issued = g.Sum(x => x.Kind == "out" ? -(long)x.Delta : 0),
                Adjusted = g.Sum(x => x.Kind == "adjustment" ? (long)x.Delta : 0)
            }).ToListAsync();
        var activity = Enumerable.Range(0, 14).Select(i =>
        {
            var date = start.AddDays(i);
            var row = grouped.SingleOrDefault(x => x.Date == date);
            return new DailyActivity(DateOnly.FromDateTime(date), row?.Received ?? 0, row?.Issued ?? 0, row?.Adjusted ?? 0);
        }).ToList();
        await tx.CommitAsync();
        return new(total, units, value, retail, low, outOfStock, categories, attention, recent, activity, asOf);
    }
}
