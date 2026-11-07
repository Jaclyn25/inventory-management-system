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
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.NormalizedName).HasMaxLength(80);
            e.HasIndex(x => x.NormalizedName).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_Category_Name", "length(btrim(\"Name\")) > 0"));
        });
    }
}
