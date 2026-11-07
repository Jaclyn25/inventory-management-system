using System.Data;
using Microsoft.EntityFrameworkCore;
using Stockroom.Api.Contracts;
using Stockroom.Api.Data;
namespace Stockroom.Api.Services;

public sealed class ProductService(InventoryDb db)
{
    public static IQueryable<ProductView> Views(IQueryable<Product> products) => products.Select(x => new ProductView(
        x.Id, x.Sku, x.Name, x.Description, x.CategoryId, x.Category.Name, x.UnitPrice, x.CostPrice,
        x.LowStockThreshold, x.Quantity, x.IsArchived, x.Version, x.CreatedAt));

    public static IQueryable<Product> Filter(IQueryable<Product> products, ProductQuery query)
    {
        products = products.Where(x => x.IsArchived == query.Archived);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            products = products.Where(x => x.Name.ToLower().Contains(term) || x.Sku.ToLower().Contains(term) || x.Description.ToLower().Contains(term));
        }
        if (query.CategoryId is Guid category) products = products.Where(x => x.CategoryId == category);
        return query.Status switch
        {
            "low" => products.Where(x => x.Quantity > 0 && x.Quantity <= x.LowStockThreshold),
            "out" => products.Where(x => x.Quantity == 0),
            "healthy" => products.Where(x => x.Quantity > x.LowStockThreshold),
            _ => products
        };
    }

    public async Task<Page<ProductView>> List(ProductQuery query)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var filtered = Filter(db.Products.AsNoTracking(), query);
        var total = await filtered.CountAsync();
        var sorted = (query.Sort, query.Descending) switch
        {
            ("sku", false) => filtered.OrderBy(x => x.Sku), ("sku", true) => filtered.OrderByDescending(x => x.Sku),
            ("quantity", false) => filtered.OrderBy(x => x.Quantity), ("quantity", true) => filtered.OrderByDescending(x => x.Quantity),
            ("value", false) => filtered.OrderBy(x => x.Quantity * x.CostPrice), ("value", true) => filtered.OrderByDescending(x => x.Quantity * x.CostPrice),
            (_, true) => filtered.OrderByDescending(x => x.Name), _ => filtered.OrderBy(x => x.Name)
        };
        var items = await Views(sorted.ThenBy(x => x.Id).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)).ToListAsync();
        await tx.CommitAsync();
        return new(items, total, query.Page, query.PageSize);
    }

    public async Task<ProductView> Get(Guid id) => await Views(db.Products.AsNoTracking().Where(x => x.Id == id)).SingleOrDefaultAsync()
        ?? throw new ApiException(404, "Product not found.");

    public async Task<ProductView> Save(ProductInput input, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) throw new ApiException(400, "Product name is required.");
        if (decimal.Round(input.UnitPrice, 2) != input.UnitPrice || decimal.Round(input.CostPrice, 2) != input.CostPrice)
            throw new ApiException(400, "Prices must have at most two decimal places.");
        if (!await db.Categories.AnyAsync(x => x.Id == input.CategoryId)) throw new ApiException(400, "Choose an existing category.");
        var product = id is null ? new Product() : await db.Products.FindAsync(id) ?? throw new ApiException(404, "Product not found.");
        if (product.IsArchived) throw new ApiException(409, "Archived products cannot be edited.");
        if (id is not null && product.Version != input.Version) throw new ApiException(409, "Product changed. Refresh before editing.");
        product.Sku = input.Sku.ToUpperInvariant();
        product.Name = input.Name.Trim();
        product.Description = input.Description?.Trim() ?? "";
        product.CategoryId = input.CategoryId;
        product.UnitPrice = input.UnitPrice;
        product.CostPrice = input.CostPrice;
        product.LowStockThreshold = input.LowStockThreshold;
        if (id is null) db.Products.Add(product); else product.Version++;
        await db.SaveChangesAsync();
        return await Get(product.Id);
    }

    public async Task Archive(Guid id, long version)
    {
        var changed = await db.Products.Where(x => x.Id == id && !x.IsArchived && x.Quantity == 0 && x.Version == version)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsArchived, true).SetProperty(x => x.Version, x => x.Version + 1));
        if (changed == 0)
        {
            if (!await db.Products.AnyAsync(x => x.Id == id)) throw new ApiException(404, "Product not found.");
            throw new ApiException(409, "Only current, active products with zero stock can be archived.");
        }
    }
}
