using Microsoft.EntityFrameworkCore;
namespace Stockroom.Api.Data;

public static class SeedData
{
    public static async Task Initialize(InventoryDb db)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(73915268)");
        if (await db.Products.AnyAsync() || await db.Categories.AnyAsync()) return;
        var categories = new[] { "Desk & stationery", "Electronics", "Packaging", "Office furniture", "Breakroom", "Safety supplies" }
            .Select(name => new Category { Name = name, NormalizedName = name.ToUpperInvariant() }).ToArray();
        db.Categories.AddRange(categories);
        string[] names = ["Dot-grid notebook", "Wireless keyboard", "Recycled mailer box", "Adjustable desk riser", "Ceramic coffee cup", "First aid refill kit", "Fine-point pen set", "USB-C docking hub", "Kraft packing tape", "Ergonomic task chair", "Stainless water bottle", "Protective work gloves", "Weekly desk planner", "Portable label printer", "Padded shipping sleeve", "Monitor stand", "Ground coffee blend", "Reflective safety vest"];
        decimal[] costs = [4.50m, 32.00m, 1.20m, 68.00m, 6.75m, 18.50m, 3.25m, 45.00m, 2.10m, 145.00m, 9.80m, 4.25m, 5.60m, 72.00m, .85m, 24.50m, 8.90m, 6.20m];
        var now = DateTime.UtcNow;
        for (var i = 0; i < names.Length; i++)
        {
            var target = i is 2 or 11 ? 0 : i is 1 or 4 or 8 or 14 ? 3 + i % 3 : 24 + i * 7;
            var product = new Product
            {
                Sku = $"NS-{1001 + i}", Name = names[i], Description = $"Fictional Northline Supply catalog item. {names[i]} for everyday workplace operations.",
                CategoryId = categories[i % 6].Id, CostPrice = costs[i], UnitPrice = decimal.Round(costs[i] * 1.6m, 2), LowStockThreshold = 10,
                CreatedAt = now.AddDays(-28), Quantity = target, Version = 3
            };
            db.Products.Add(product);
            var opening = target + 12 + i % 5;
            db.Movements.Add(new StockMovement
            {
                Id = Guid.NewGuid(), IdempotencyKey = Guid.NewGuid(), Fingerprint = "SEED", ProductId = product.Id,
                Sku = product.Sku, ProductName = product.Name, Kind = "in", Delta = opening, QuantityBefore = 0, QuantityAfter = opening,
                CostPrice = costs[i], Reason = "Fictional opening delivery", CreatedAt = now.AddDays(-12 + i % 5)
            });
            db.Movements.Add(new StockMovement
            {
                Id = Guid.NewGuid(), IdempotencyKey = Guid.NewGuid(), Fingerprint = "SEED", ProductId = product.Id,
                Sku = product.Sku, ProductName = product.Name, Kind = "out", Delta = target - opening, QuantityBefore = opening, QuantityAfter = target,
                CostPrice = costs[i], Reason = "Fictional workplace dispatch", CreatedAt = now.AddDays(-6 + i % 6).AddHours(-i)
            });
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
