using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Stockroom.Api.Contracts;
using Stockroom.Api.Data;
namespace Stockroom.Api.Services;

public sealed class StockService(InventoryDb db, ILogger<StockService> logger)
{
    public static IQueryable<MovementView> Views(IQueryable<StockMovement> query) => query.Select(x => new MovementView(
        x.Id, x.ProductId, x.Sku, x.ProductName, x.Kind, x.Delta, x.QuantityBefore, x.QuantityAfter, x.CostPrice, x.Reason, x.CreatedAt));

    public async Task<MovementView> Receive(StockInput input, Guid key)
    {
        if (input.OperationId == Guid.Empty || input.ProductId == Guid.Empty || key == Guid.Empty)
            throw new ApiException(400, "Operation, product and idempotency IDs must be nonempty UUIDs.");
        var reason = input.Reason.Trim();
        if (reason.Length < 3) throw new ApiException(400, "Provide a meaningful stock reason.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Kind = "in", input.OperationId, input.ProductId, input.Quantity, Reason = reason }))));
        await using var tx = await db.Database.BeginTransactionAsync();
        // Serialize retries for a key before locking a product. Hash collisions only cause extra waiting.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key.ToString()}, 0))");
        var previous = await db.Movements.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key);
        if (previous is not null)
        {
            if (previous.Fingerprint != fingerprint) throw new ApiException(409, "Idempotency key was already used for a different operation.");
            await tx.CommitAsync();
            return View(previous);
        }
        var product = await db.Products.FromSqlInterpolated($"SELECT * FROM \"Products\" WHERE \"Id\" = {input.ProductId} FOR UPDATE").SingleOrDefaultAsync()
            ?? throw new ApiException(404, "Product not found.");
        if (product.IsArchived) throw new ApiException(409, "Archived products cannot receive stock movements.");
        if (input.Quantity > 1000000000 - product.Quantity) throw new ApiException(400, "Stock exceeds the supported quantity limit.");
        var before = product.Quantity;
        product.Quantity += input.Quantity;
        product.Version++;
        var movement = new StockMovement
        {
            Id = input.OperationId, IdempotencyKey = key, Fingerprint = fingerprint, ProductId = product.Id,
            Sku = product.Sku, ProductName = product.Name, Kind = "in", Delta = input.Quantity,
            QuantityBefore = before, QuantityAfter = product.Quantity, CostPrice = product.CostPrice, Reason = reason
        };
        db.Movements.Add(movement);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        logger.LogInformation("Stock operation {OperationId} {Kind} product {ProductId} delta {Delta}", movement.Id, movement.Kind, product.Id, movement.Delta);
        return View(movement);
    }

    private static MovementView View(StockMovement x) => new(x.Id, x.ProductId, x.Sku, x.ProductName, x.Kind, x.Delta, x.QuantityBefore, x.QuantityAfter, x.CostPrice, x.Reason, x.CreatedAt);

    public async Task<Page<MovementView>> History(MovementQuery query)
    {
        if (query.From > query.To) throw new ApiException(400, "From must be before To.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var rows = db.Movements.AsNoTracking().AsQueryable();
        if (query.ProductId is Guid product) rows = rows.Where(x => x.ProductId == product);
        if (query.Kind is not null) rows = rows.Where(x => x.Kind == query.Kind);
        if (query.From is not null) { var from = query.From.Value.UtcDateTime; rows = rows.Where(x => x.CreatedAt >= from); }
        if (query.To is not null) { var to = query.To.Value.UtcDateTime; rows = rows.Where(x => x.CreatedAt <= to); }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            rows = rows.Where(x => x.Sku.ToLower().Contains(term) || x.ProductName.ToLower().Contains(term) || x.Reason.ToLower().Contains(term));
        }
        var total = await rows.CountAsync();
        var items = await Views(rows.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)).ToListAsync();
        await tx.CommitAsync();
        return new(items, total, query.Page, query.PageSize);
    }
}
