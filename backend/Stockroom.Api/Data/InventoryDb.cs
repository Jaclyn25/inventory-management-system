using Microsoft.EntityFrameworkCore;

namespace Stockroom.Api.Data;

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
}

public sealed class InventoryDb(DbContextOptions<InventoryDb> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> Movements => Set<StockMovement>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<StockMovement>(e =>
        {
            e.ToTable("StockMovements", t =>
            {
                t.HasCheckConstraint("CK_Movement_Balance", "\"QuantityBefore\" >= 0 AND \"QuantityAfter\" BETWEEN 0 AND 1000000000 AND \"QuantityAfter\" = \"QuantityBefore\" + \"Delta\"");
                t.HasCheckConstraint("CK_Movement_Kind", "(\"Kind\" = 'in' AND \"Delta\" > 0) OR (\"Kind\" = 'out' AND \"Delta\" < 0) OR (\"Kind\" = 'adjustment' AND \"Delta\" <> 0)");
                t.HasCheckConstraint("CK_Movement_Reason", "length(btrim(\"Reason\")) BETWEEN 3 AND 500");
            });
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.HasIndex(x => new { x.ProductId, x.CreatedAt });
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.Fingerprint).HasMaxLength(64);
            e.Property(x => x.Sku).HasMaxLength(40);
            e.Property(x => x.ProductName).HasMaxLength(160);
            e.Property(x => x.Kind).HasMaxLength(16);
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.CostPrice).HasPrecision(14, 2);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.NormalizedName).HasMaxLength(80);
            e.HasIndex(x => x.NormalizedName).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Category_Name", "length(btrim(\"Name\")) > 0"));
        });
        model.Entity<Product>(e =>
        {
            e.Property(x => x.Sku).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.UnitPrice).HasPrecision(14, 2);
            e.Property(x => x.CostPrice).HasPrecision(14, 2);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasIndex(x => x.Sku).IsUnique();
            e.HasIndex(x => new { x.IsArchived, x.Name });
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Product_Quantity", "\"Quantity\" BETWEEN 0 AND 1000000000");
                t.HasCheckConstraint("CK_Product_Threshold", "\"LowStockThreshold\" BETWEEN 0 AND 1000000000");
                t.HasCheckConstraint("CK_Product_Prices", "\"UnitPrice\" BETWEEN 0 AND 999999999.99 AND \"CostPrice\" BETWEEN 0 AND 999999999.99");
                t.HasCheckConstraint("CK_Product_Identity", "\"Sku\" ~ '^[A-Z0-9][A-Z0-9_-]{1,39}$' AND length(btrim(\"Name\")) > 0");
                t.HasCheckConstraint("CK_Product_Archive", "NOT \"IsArchived\" OR \"Quantity\" = 0");
            });
        });
    }
}
