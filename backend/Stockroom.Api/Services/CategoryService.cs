using Microsoft.EntityFrameworkCore;
using Stockroom.Api.Contracts;
using Stockroom.Api.Data;
namespace Stockroom.Api.Services;

public sealed class CategoryService(InventoryDb db)
{
    public Task<List<Category>> List() => db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
    public async Task<Category> Save(CategoryInput input, Guid? id = null)
    {
        var name = input.Name.Trim();
        if (name.Length == 0) throw new ApiException(400, "Category name is required.");
        var category = id is null ? new Category() : await db.Categories.FindAsync(id) ?? throw new ApiException(404, "Category not found.");
        category.Name = name;
        category.NormalizedName = name.ToUpperInvariant();
        if (id is null) db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }
    public async Task Delete(Guid id)
    {
        var category = await db.Categories.FindAsync(id) ?? throw new ApiException(404, "Category not found.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }
}
